## 오퍼레이터

스트림을 조작하는 메소드

종류는 엄청나게 많다.

Select, Where, Skip, SkipUntil, SkipWhile, Take, TakeUntil, TakeWhile, Throttle, Zip,
Merge, CombineLatest, Distinct, DistinctUntilChanged, Delay, DelayFrame, First,
FirstOfDefault, Last, LastOfDefault, StartWith, Concat, Buffer, Cast, Catch,
CatchIgnore, ObserveOn, Do, Sample, Scan, Single, SingleOrDefault, Retry, Repeat,
Time, TimeStamp, TimeInterval... 등등

### 자주 사용 하는 오퍼레이터

### 1. Where

조건을 만족하는 메세지만 통과 시키는 오퍼레이터

> 다른 언어에서는 \[filter]라고도 한다.

![Rx where](https://user-images.githubusercontent.com/67315288/121776437-c80a9800-cbc7-11eb-8ed7-dc4c3634d648.png)

### 2. Selet

요소의 값을 변경한다

> 다른 언어에서는 \[map]이라고 한다.

![Rx Select](https://user-images.githubusercontent.com/67315288/121776427-c640d480-cbc7-11eb-9cc5-78c771ccd6d7.png)

### 3. SelectMany

새로운 스트림을 생성하고, 그 스트림이 흐르는 메세지를 본래의 스트림의 메세지로 취급

> 스트림을 다른 스트림으로 교체하는 이미지 ( 정밀히 말하면 다름 )
> 다른 언어에서는 \[flatMap]이라고도 한다.

![Rx SelectMany](https://user-images.githubusercontent.com/67315288/121776429-c640d480-cbc7-11eb-8b1a-7097d499f238.png)

### 4. Throttle / ThrottleFrame

도착한 때에 최후의 메세지를 보낸다

> 메시지가 집중해서 들어 올때에 마지막 이외를 무시한다
> 다른 언어에서는 [debounce]라고도 한다
> 자주 사용됨

![Rx Throttle_ThrottleFrame](https://user-images.githubusercontent.com/67315288/121776434-c7720180-cbc7-11eb-9169-8817db8448b3.png)

### 5. ThrottleFirst / ThrottleFirstFrame

최초의 메시지가 올때부터 일정 시간 무시 한다

> 하나의 메시지가 온때부터 잠시 메세지를 무시 한다
> 대용량으로 들어오는 데이터의 첫번째만 사용하고 싶을 때 유효

![Rx ThrottleFirst_ThrottleFirstFrame](https://user-images.githubusercontent.com/67315288/121776435-c80a9800-cbc7-11eb-910a-fca20c1941bc.png)

### 6. Delay / DelayFrame

메시지의 전달을 연기 한다

![Rx Delay_DelayFrame](https://user-images.githubusercontent.com/67315288/121776438-c8a32e80-cbc7-11eb-8e1e-97b9c1befbea.png)

### 7. DistinctUntilChanged

메세지가 변화한 순간에만 통지한다

> 같은 값이 연속되는 경우에는 무시한다

![Rx DistinctUntilChanged](https://user-images.githubusercontent.com/67315288/121776423-c50fa780-cbc7-11eb-9761-d433dc9fb2e6.png)

### 8. SkipUntil

지정한 스트림에 메시지가 올때까지 메시지를 Skip 한다

> 같은 값이 연속되는 경우에 무시한다

![Rx SkipUntil](https://user-images.githubusercontent.com/67315288/121776430-c6d96b00-cbc7-11eb-9a3b-94fd58e82309.png)

### 9. TakeUntil

지정한 스트림에 메시지가 오면, 자신의 스트림에 OnCompleted를 보내서 종료 한다

![Rx TakeUntil](https://user-images.githubusercontent.com/67315288/121776433-c7720180-cbc7-11eb-8d13-43b1cc762fa1.png)

### 10. Repeat

스트림이 OnCompleted로 종료될 때에 다시 한번 Subscribe를 한다

### 11. First

스트림에 최초로 받은 메시지만 보낸다

> OnNext 직후에 OnComplete도 보낸다

![Rx First](https://user-images.githubusercontent.com/67315288/121776426-c5a83e00-cbc7-11eb-9c10-094c29358af2.png)

### 그외에 자주 사용되는 조합

1. SkipUntil + TakeUntil + Repeat

이벤트 A가 올때부터 이벤트 B가 올때까지 처리를 하고 싶을때 사용

> ex) 드래그로 오브젝트를 회전 시키기
> MouseDown이 올 때부터 Mouse Up이 올때까지 처리할 때

![Rx SkipUntil+TakeUntil+Repeat](https://user-images.githubusercontent.com/67315288/121776432-c7720180-cbc7-11eb-9194-7bb9b9bb8b28.png)
