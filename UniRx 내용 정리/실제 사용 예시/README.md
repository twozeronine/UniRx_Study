# 실제 사용 예 5가지

## 1. 더블 클릭 판정

```C#

using UniRx.Trigger

var clickStream = UpdateAsObservable() // 클릭 스트림을 정의 ( 생성 아님 )
      .Where( _ => Input.GetMouseButtonDown(0));

clickStream.Buffer( clickStream.Throttle(TimeSpan.FromMilliseconds(200)))
      .Where( x => x.Count >= 2 )
      .SubscribeToText( _text, x =>
          string.Format("DoubleClick detected! \n Count: {0}", x.Count));
```

## 2. 값의 변화를 감시하기

플레이어가 지면에 떨어지는 순간에 이펙트를 발생

지면에 떨어진 순간의 감지 방법

1. CharacterController.isGrounded를 매프레임 체크
2. 현재 프레임의 값을 필드 변수에 저장
3. 매프레임에 False -> True로 변화할 때에 이펙트를 재생한다.

```C#
public class OnGroundedScript : ObservableMonoBehaviour
{
  public override void Start()
  {
    var characterController = GetComponent<CharacterController>();
    var particleSystem = GetComponentInChildren<ParticleSystem>();

    UpdateAsObservable()
        .Select(_=> characterController.isGrounded)
        .DistinctUntilChanged()
        .Where( x => x )
        .Subscribe(_ => particleSystem.Play());

   // 매 프레임 값의 변화를 감시한다면 [ObserveEveryValueChanged]의 쪽이 심플하다
   //ObserveEveryValueChanged
   characterController
      .ObserveEveryValueChanged( x => x.isGrounded )
      .Where( x => x )
      .Subscribe( _ => Debug.Log("OnGrounded"));
  }
}


```

## 3. 값의 변화를 가다듬기

isGrounded의 변화를 다듬기

곡면을 이동하게 되면 True/False가 격렬하게 변환한다 그래서 어느정도 다듬어 정말 변화가 크게 일어날때만 값을 체크한다.

isGrounded의 변화를 Throttle로 무시한다

- DistinctUntilChanged와 같이 쓰면 OK

```C#

UpdateAsObservable()
    .Select(_=> playerCharacterController.isGrounded)
    .DistinctUntilChanged()
    .ThrottleFrame(5)
    .Subscribe( x => throttledIsGrounded = x );

```

![Rx 변화 가다듬기](https://user-images.githubusercontent.com/85855054/122054848-05b72d00-ce23-11eb-9822-593ee5b2bbc1.png)

## 4. WWW를 사용하기 쉽게 하기

기존의 Unity가 제공하는 HTTP 통신용 모듈

- 코루틴을 사용할 필요가 있다
- 사용 편의성이 떨어짐

WWW를 Observable로써 취급할 수 있게 한것

- Subscribe된 순간에 통신을 실행한다
- 나중에 알아서 뒤에서 통신한 결과를 스트림에 보내 준다
- 코루틴을 사용하지 않아도 된다!

```C#
button.OnClickAsObservable()
    .First() // 버튼을 연타하여도 통신은 1회만 하도록 First를 넣음.
    .SelectMany(ObservableWWW.GetWWW(resourceURL)) // 클릭 스트림을 ObservableWWW의 스트림으로 덮어쓴다
    .Select( www => Sprite.Create(www.texture, new Rect(0,0,400,400), Vector2.zero))
    .Subscribe(sprite =>
    {
      image.sprite = sprite;
      button.interactable = false;
    }, Debug.LogError);
```

1. 버튼이 클릭되면
2. HTTP로 텍스쳐를 다운로드 해서
3. 그 결과를 Sprite로 변화해서
4. Image로 표시한다

```C#
button.OnClickAsObservable()
    .First() // 버튼을 연타하여도 통신은 1회만 하도록 First를 넣음.
    .SelectMany(ObservableWWW.GetWWW(resourceURL)).Timeout(TimeSpan.FromSeconds(3)) // 타임 아웃이 필요하다면 오퍼레이터 추가
    .Select( www => Sprite.Create(www.texture, new Rect(0,0,400,400), Vector2.zero))
    .Subscribe(sprite =>
    {
      image.sprite = sprite;
      button.interactable = false;
    }, Debug.LogError);
```

```C#
// 동시에 통신해서 모든 데이터가 모이면 처리를 진행한다
var parallel = Observable.WhenAll(
    ObservableWWW.Get("http://google.com/"),
    ObservableWWW.Get("http://bing.com/"),
    ObservableWWW.Get("http://unity3d.com/"));
)

parallel.Subscribe(xs =>
{
  Debug.Log(xs[0].Substring(0,100)); // google
  Debug.Log(xs[1].Substring(0,100)); // bing
  Debug.Log(xs[2].Substring(0,100)); // unity
})

```

```C#
// 앞의 통신결과를 사용해서 다음 통신을 실행한다

var resoucePathURL = "http://torisoup.net/unirx/resourcepath.txt"; // 이 경로에 리소스의 URL이 있음

ObservableWWW.Get(resoucePathURL)
    .SelectMany(resourceUrl => ObservableWWW.Get(resourceUrl)) // 서버에서 가르쳐준 URL로부터 데이터를 다운로드한다.
    .Subscribe(Debug.Log);

```

## 5. 기존 라이브러리를 스트림으로 변환하기 (PhtonCloud 예제)

Unity에서 간단히 네트워크 대전이 구현 가능한 라이브러리 통지가 전부 콜백이어서 미묘하게 사용하기 나쁨

![Rx 포톤 활용](https://user-images.githubusercontent.com/85855054/122058587-bbd04600-ce26-11eb-8076-4f39268dd88c.png)

콜백으로부터 스트림을 변경하는 메리트

코드가 명시적이 된다

- Photon의 콜백은 SendMessage로 호출된다
- Observable를 제대로 정희해서 사용하면 명시적이 된다

다양한 오퍼레이터에 의한 유연한 제어가 가능해 지게 된다

- 로그인이 실패하면 3초후에 리트라이를 시도한다던가
- 유저들로부터 요청이 있을때 처리를 한다던가
- 방정보 리스트가 갱신될떄 통지 한다던가

ex) 최신 방정보를 통신하는 스트림을 만들자

OnReceivedRoomListUpdate

> PhotonNetwork.GetRoomList()가 변경될때에 실행 된다

```C#

public class PhotonRoomListModel : MonoBehaviour
{
  public IObservable<RoomInfo[]> CurrentRoomsObservable
  {
    get { return _reactiveRooms.AsObservable(); }
  }

  private ReactiveProperty<RoomInfo[]> _reactiveRooms
      = new ReactiveProperty<RoomInfo[]> (new RoomInfo[0]);

  private void OnReceivedRoomListUpdate()
  {
    _reactiveRooms.Value = PhotonNetwork.GetRoomList();
  }
}

public class RoomsViewer : MonoBehaviour
{
  [SerializeField] private PhotonRoomListModel roomModel;

  void Start()
  {
    roomModel
        .CurrentRoomObservable // 갱신된 RoomInfo[] 가 흐르는 스트림
        .Subscribe(rooms =>   // 이것을 Subscribe 하게 되면, 방리스트의 갱신이 있을때마다 즉시 알게됨
        {
            //room
        })
  }
}

```

## 6. 애니메이션 동기화하기

유니티짱이 공을 던진다

- 애니메이션에 동기화 시켜서 던진다
- 던지는 공의 파라미터도 지정 가능하다

![Rx 공던지기 모션](https://user-images.githubusercontent.com/67315288/122390387-e51ade80-cfac-11eb-9088-6cd6e9623841.png)
![Rx 공던지기 모션 이벤튼](https://user-images.githubusercontent.com/67315288/122390395-e6e4a200-cfac-11eb-9019-41b42b8f545f.png)

### UniRx를 사용하지 않고 작성하기

```C#
private int _ballSpeed;

private Vector3 _ballDirection;

// 공을 던지는 처리를 시작한다

private void StartThrowBall(int speed, Vector3 direction )
{
  _ballSpeed = speed;
  _ballDirection = direction;
  _animator.SetTrigger("TriggerBallThrow"); // 여기에서 Animation 재생 시작
}

// AnimationEvent의 콜백

private void BallThrowEvent()
{
  // 공을 생성해서 던진다
  var ball = CreateBallAndChangeVelocity( _ballSpeed, _ballDirection );
  // 5초후 소멸한다
  Destroy(ball, 5.0f);
}

```

분명 하나의 연결된 처리인데도 분리된 느낌이 든다.

### StartAsCoroutine으로 작성하기

```C#
private Subject<Unit> _animationEventSubject = new Subject<Unit>();

// AnimationEvent의 콜백

private void BallThrowEvent()
{
  _animationEventSubject.OnNext(Unit.Default); // AnimationEvent를 Observable화
}

// 공을 던지는 일련의 처리를 하는 코루틴
// 콜백이 들어간 비동기처리를 동기처리처럼 쓸수 있다
private IEnumerator ThrowBallCoroutine(int speed, Vector3 direction)
{
  var waitStream = _animationEventSubject.FirstOrDefault().Replay();
  waitStream.Connect();

  // 애니메이션 시작
  _animator.SetTrigger("TriggerBallThrow");

  // 애니메이션 이벤트가 올때까지 대기
  yield return waitStream.StartAsCoroutine();

  var ball = CreateBallAndChangeVelocity(speed, direction);
  Destroy(ball, 5.0f );
}

```

## 7. Subject\<T>

### 스트림의 소스를 만드는것

- Subject, ReplySubject, BehaviorSubject, AsyncSubject로 여러 종류 있다
- 외부에 공개할 때에는 반드시 AsObservable을 거쳐서 공개한다

* 외부에서 직접 OnNext가 호출되는 상태로 하지 않음

```C#
Subject<int> stream = new Subject<int>();

stream
    .Where( x => x > 10 )
    .Subscribe( x => Debug.Log(x));

stream.OnNext(1);
stream.OnNext(20);
stream.OnNext(30);

stream.OnCompleted();
```

### Subject를 멈추는 법

Dispose를 호출하면 Subscribe가 중지 된다

- 스트림의 소스가 해제되면 자동적으로 Dispose 된다
- 스트림이 완료상태가 되어도 Dispose 된다
- static한 스트림을 만들 경우에는 수동 Dispose가 필요

```C#
IDisposable disposable = button.onClick
    .AsObservable()
    .Subscribe( _ => text.text = "clicked");

disposable.Dispose();

```

## 보충 팁) UpdateAsObservable과 Observable.EveryUpdate

둘다 Update()의 타이밍에 통지되는 Observable

- UpdateAsObservable
  -- Observable을 계승하는 방식은 없어졌음 대신에 UniRx.Trigger 네임 스페이스에 준비된 확장 메소드를 사용
  -- IObservable\<Unit>
  -- 컴포넌트가 Destroy 될때 자동 Dispose

- Observable.EveryUpdate
  -- 어느 스크립트에서든 사용할 수 있다
  -- IObservable\<long> (Subscribe된 때부터의 프레임 수)
  -- 사용이 끝나면 명시적으로 Dispose 할 필요가 있다 혹은 OnCompleted가 제대로 불리는 스트림으로 한다

### 코루틴을 Observable로 변환하기

Observable.FromCoroutine을 사용해서 변경 가능

- 코루틴을 실행순서나 실행 조건을 스트림으로 정의 가능

```C#
private void Start()
{
  Observable.FromCoroutine(CoroutineA)
      .SelectMany(Observable.FromCoroutine(CoroutineB))
      .Subscribe( _ => Debug.Log("CoroutineA & CoroutineB are Done."));
}

private IEnumerator CoroutineA()
{
  Debug.Log("CoroutineA start");
  yield return new WaitForSeconds(1);
  Debug.Log("CoroutineA end");
}

private IEnumerator CoroutineB()
{
  Debug.Log("CoroutineB start");
  yield return new WaitForSeconds(1);
  Debug.Log("CoroutineB end");
}

```

### UniRx + 코루틴

FromCoroutine\<T>를 사용하면 자유롭게 스트림을 만들 수 있다

```C#
private void Start()
{
  Observable.FromCoroutine<int>(observer => TimerCoroutine(observer,10))
      .Subscribe( _ => Debug.Log(_));
}

private IEnumerator TimerCoroutine(IObserver<int> observer, int timeCount )
{
  do
  {
    observer.OnNext(timeCount);
    yield return new WaitForSeconds(1.0f);
  } while ( --timeCount > 0 );

  observer.OnNext(timeCount); // 카운트다운 타이머의 예
  observer.OnCompleted();
}

```

하지만 FromCoroutine\<T>로 카운트 다운 타이머를 만드는것보다 Observable.Timer로 만드는것이 스마트하다

```C#
private void Start()
{
  createCountDownObservable(10)
    .Subscribe( x => Debug.Log(x), () => Debug.Log("OnComplete"));
}

private IObservable<int> createCountDownObservable(int time)
{
  return Observable.Timer(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1))
      .Select( x => (int) (tiem - x ))
      .Take(time + 1 );
}
```

### ObserveOn

처리를 실행하는 스레드를 교체하는 오퍼레이터

- ObserveOn을 사용하는 스레드간에 데이터의 취급을 고려할 필요가 없어진다

```C#
_button.OnClickAsObservable()
    .ObserveOn(Scheduler.TreadPool) // 여기서부터 스레드 풀에서 실행
    .Select( _ => LoadFile())       // 무지하게 무거운 파일을 로드
    .ObserveOnMainTread()           // 메인 스레드로 복귀
    .Subscribe(data =>
    {
      //Do Someting using data
    });

```

### 사용 예) 텍스트가 입력되면 검색 서제스트를 뛰우기

1. InputField에 텍스트를 입력 받을 때
2. 최후에 입력된 때부터 200m초 이상 간극이 생기면
3. GoogleSuggestAPI를 호출해서
4. 그때의 서제스틀 결과를 Text 표시한다

```C#

private readonly string _apiUrlFormat
  ="http://www.google.com/complete/search?hl=ja&output=toolbar&q={0}";

private void Start()
{
  _inputField
      .OnvalueChangeAsObservable()
      .Throttle(TimeSpan.FromMilliseconds(200))
      .Where(word => word.Legth > 0 )
      .SelectMany( word => ObservableWWW.Get(string.Format(_apiUrlFormat, WWW.EscapeURL(word))))
      .Select(xml => XMLResultToStrings(xml))
      .Where(suggests => suggests.Any())
      .SubscribeToText( _text, suggestResults =>
          suggestResults.Aggregate((s,n)=> s + n + System.Environment.NewLine)
      );
}

```
