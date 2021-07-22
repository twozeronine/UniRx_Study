# Hot 변환 한 포인트

여러 상황이 있지만 가장 Hot 변환이 중요해지는 상황은 하나의 스트림을 여러 번 Subscribe하는 경우 이다.

## 예) 입력된 문자열이 특정 키워드와 일치하는지 검사

Hot 변환이 필요한 예로 "입력된 키 입력을 감지하고 4 문자의 특정 키워드가 입력되었는지를 알아 내는 스트림"을 만들기

```C#
var KeyBufferStream
    = Observable.FromEvent<KeyEventHandler, KeyEventArgs>(
        h => (sender, e) => h(e),
        h => KeyDown += h,
        h => KeyDown -= h)
        .Select( x => x.Key.ToString()) // 입력 키를 문자로 변환
        .Buffer(4, 1) // 4 개씩 정리
        .Select( x => x.Aggregate((p,c) => p + c )); // 문자에서 문자열로 변환

KeyBufferStream.Subscribe(Console.WriteLine);

/*실행결과
> ABCDEFHGH 입력

ABCD
BCDE
CDEF
DEFG
EFGH
*/

```

이같이 keyBufferStream은 입력 키가 4자씩 뭉치고 흐르는 스트림이다.

```C#
// 유니티에서 실행 가능한 예제
var KeyBufferStream = this.UpdateAsObservable()
     .Where(_ => Input.anyKeyDown) //아무 버튼 눌렀을 때
     .Where(_ => !(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDonw(1) || Input.GetMouseButtonDown(2))) // 마우스는 무시
     .Select(_ => Input.inputString) // 버튼 스트링
     .Buffer(4, 1) // 4개씩 정리
     .Select(x => x.Aggregate((p, c) => p + c)); // 문자에서 문자열로 변환

// 결과 표시
KeyBufferStream.Subscribe(Debug.Log);

```

Aggregate는 Linq 에서 지원 하는 메서드이며, 집계 연산자이다. ( 누산기 )

## KeyBufferStream을 사용하여 "HOGE" 또는 "FUGA"의 입력을 감시하자

Where 사이 HOGE와 FUGA에서 2회 Subscribe 한다.

```C#
keyBufferStream.Where(x => x == "HOGE")
    .Subscribe(_ => Debug.Log("Input HOGE"));

keyBufferStream.Where(x => x == "FUGA")
    .Subscribe(_ => Debug.Log("Input FUGA"));

```

실행 결과 (HOGEFUGA 입력 한 결과)

```C#
Input HOGE
Input FUGA
```

![unirx hot 분기](https://user-images.githubusercontent.com/85855054/126576760-ecbdccd8-e8e5-4df3-9301-c771674911ff.jpeg)

각각의 문자열에 반응하는 스트림을 만들어 Subscribe 할 수 있었다

하지만 이 스트림에는 커다란 문제가 있다

## 문제점

상기 스트림의 문제점은 KeyBufferStream이 Cold Observable로 형성되는 것이 문제이다.  
Cold Observable은 분기하지 않는데, Subscribe 할때마다 매번 새로운 스트림을 생성하는 특성이 있다.

따라서 위와 같이 작성을 하면 다음과 같은 문제가 발생한다.

-   뒤에서 다중 스트림이 생성되어 버린다. 메모리와 CPU를 낭비한다.
-   Subscribe한 시점에서 따라 흘러 나오는 결과가 다르다 [Cold Observable의 성질](https://github.com/twozeronine/UniRx_Study/tree/main/UniRx%20%EC%9B%90%EB%AC%B8%20%EB%82%B4%EC%9A%A9/Rx%EC%9D%98%20Hot%EA%B3%BC%20Cold%EC%97%90%20%EB%8C%80%ED%95%B4)  
    -- Cold Observable은 Subscribe 한 순간부터 오퍼레이터가 작동하게 된다.
    -- Subscribe 전에 온 메시지는 모든 처리 조차 되지 않고 소멸 된다.

스트림이 2개로 흐른다는 증거 예시

```C#
var keyBufferStream
    = Observable.FromEvent<KeyEventHandler, KeyEventArgs>(
        h => (sender, e) => h(e),
        h => KeyDown += h,
        h => KeyDown -= h)
        .Select(x => x.Key.ToString())
        .Buffer(4, 1)
        .Do(_=> Console.WriteLine("Buffered")) // Buffer가 OnNext를 방출한 타이밍에 출력된다.
        .Select(x => x.Aggregate((p, c) => p + c));

keyBufferStream
    .Where(x => x == "HOGE")
    .Subscribe(_ => Console.WriteLine("Input HOGE"));

keyBufferStream
    .Where(x => x == "FUGA")
    .Subscribe(_ => Console.WriteLine("Input FUGA"));

/*실형결과 (AAAA와 Buffer가 1번만 움직이도록 키 입력 )
Buffered
Buffered // Buffer는 1회만 흐르고 있는데 2번 출력된다 = 즉 스트림이 2개로 흐르고 있다.
*/

```

![unirx hot 분기 2](https://user-images.githubusercontent.com/85855054/126577035-0db6b7db-18be-4bb5-aa7d-87dd17449c38.jpeg)

Hot Observable이 스트림의 근원인 FromEvent 밖에 없기 때문에, Subscribe 할 때마다 FromEvent로부터 새롭게 스트림이 생성되어 버리는 움직임을 한다.

# 문제의 해결책 "Hot 변환"

Hot 변환은 하나의 스트림을 동시에 여러 Subscribe하는 경우에 사용한다

즉 Hot 변환하여 스트림의 분기점을 만들어 여러 Subscribe 했을 때 스트림을 하나로 통합 할 수 있게 되는 것입니다.

![unirx hot 분기 3](https://user-images.githubusercontent.com/85855054/126578042-42953fba-518d-474d-9b14-9011622fee87.jpeg)

```C#
var keyBufferStream
    = Observable.FromEvent<KeyEventHandler, KeyEventArgs>(
        h => (sender, e) => h(e),
        h => KeyDown += h,
        h => KeyDown -= h)
        .Select(x => x.Key.ToString())
        .Buffer(4, 1)
        .Select(x => x.Aggregate((p, c) => p + c))
        .Publish() // Publish에서 Hot 변환(Publish가 대표하여 Subscribe 해 준다)
        .RefCount(); // RefCount은 Observer가 추가되었을 때 자동 Connect 해 주는 오퍼레이터.

keyBufferStream
    .Where(x => x == "HOGE")
    .Subscribe(_ => Console.WriteLine("Input HOGE"));

keyBufferStream
    .Where(x => x == "FUGA")
    .Subscribe(_ => Console.WriteLine("Input FUGA"));

/* 실행 결과
Input HOGE
Input FUGA
*/

```

Hot 변환 방식에는 여러 가지가 있지만 가장 쉬운 것이 Publish()와 RefCount()를 결합하는 것이다.

Publish와 RefCount의 설명은 [여기](http://introtorx.com/Content/v1.0.10621.0/14_HotAndColdObservables.html#HotAndCold)

# 정리

-   스트림을 의도적으로 분기 하고 싶을 때 Hot 변환을 수행 한다.
-   스트림을 생성하여 반환하는 속성과 함수를 정의하면 끝에 Hot 변환을 하는 것이 안전하다.
-   Hot 변환을 잊어 버리면 메모리나 CPU가 낭비되거나 Susbscribe 타이밍이 어긋날 수 있다.
-   Hot 변환 오퍼레이터는 몇 개 있지만, Publish() + RefCount()의 조합이 편리하다. ( 만능은 아님 )

[원문 출처](https://qiita.com/toRisouP/items/c955e36610134c05c860)
