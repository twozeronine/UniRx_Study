# UniRx의 사용예

## Update를 없애기

- Update()를 Observable로 변환해서 Awake()/Start()에 모아서 작성하기

Observable화 하지 않은 구현

```C#
private void Update()
{
  if(canPlayerMove)
  {
    var inputVector = (new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")));

    if(inputVector.magnitude > 0.5f )
      Move(inputVector.normalized);

    if(isOnGrounded && Input.GetButtonDown("Jump"))
      Jump();
  }

  if( ammoCount > 0 )
  {
    if ( Input.GetButtonDown("Attack"))
      Attack();
  }
}

```

비교 Observable

작업을 병렬로 작성할 수 있어서 읽기가 쉽다

```C#
private void Start()
{
    //이동 가능할때에 이동키가 일정 이상 입력 받으면 이동
    this.UpdateAsObservable()
        .Where(_=> canPlayerMove )
        .Select(_ => new Vector3(Input.GetAxis("Horizontal"),0,Input.GetAxis("Vertical")))
        .Where(input => input.magintude > 0.5f)
        .Subscribe(Move);

    //이동 가능하고, 지면에 있을때에 점프 버튼이 눌리면 점프
    this.UpdateAsObservable()
        .Where(_ => canPlayerMove && isOnGrounded && Input.GetButtonDown("Jump"))
        .Subscribe(_ => Jump());

    // 총알이 있는 경우 공격 버튼이 눌리면 공격
    this.UpdateAsObservable()
        .Where( _ => ammoCount > 0 && Input.GetButtonDown("Attack"))
        .Subscribe( _ => Attack());
}
```

### Update를 없애기의 메리트

작업별로 병렬로 늘어서 작성하는것이 가능

- 작업별로 스코프가 명시적이게 된다
- 기능 추가, 제거, 변경이 용이해지게 된다
- 코드가 선언적이 되어서, 처리의 의도가 알기 쉽게 된다

Rx의 오퍼레이터가 로직 구현에 사용 가능

- 복잡한 로직을 오퍼레이터의 조합으로 구현 가능하다

### Observable로 변경하는 3가지 방법

UpdateAsObaservable

- 지정한 gameObject에 연동된 Observable가 만들어진다.
- gameObject의 Destroy때에 OnCompleted가 발행된다

Observable.EveryUpdate

- gameObject로부터 독립된 Observable이 만들어 진다
- MonoBehaviour에 관계 없는 곳에서도 사용 가능

ObserveEveryValueChanged

- Observable.EveryUpdate의 파생 버전
- 값의 변화를 매프레임 감시하는 Observable이 생성된다

### Observable.EveryUpdate의 주의점!!

Destroy때에 OnCompleted가 발생되지 않는다

- UpdateAsObaservable과 같은 느낌으로 사용하면 함정에 빠진다

```C#
  [SerializeField] private Text text;
  private void Start()
  {
    Observable.EveryUpdate()
        .Select(_ => tranform.position )
        .SubscribeToText(text); //이 gameObject가 파괴되면 Null이 되어 예외가 발생한다
  }
```

### 수명 관리의 간단한 방법

AddTo

- 특정 gameObject가 파괴되면 자동 Dispose 되게 해준다
- OnCompleted가 발행되는것은 아니다

```C#
  [SerializeField] private Text text;
  private void Start()
  {
    Observable.EveryUpdate()
        .Select(_ => tranform.position )
        .SubscribeToText(text)
        .AddTo(this.gameObject); // AddTo로 넘겨진 gameObject가 Destroy되면 같이 Dispose 된다
  }
```

## 컴포넌트를 스트림으로 연결하기

컴포넌트를 스트림으로 연결하는 것으로, Observer패턴한 설계로 만들 수 있다

- 전체가 이벤트 기반이 되게 할 수 있다
- 더불어 Rx는 Observer패턴 그 자체

타이머의 카운트를 화면에 표시 한다

- UniRx를 사용하지 않고 구현

```C#
public class TimerDisplayComponent : MonoBehaviour
{
  [SerializeField]
  private TimerComponent _timerComponent;
  private Text _timerText;

  void Start()
  {
    _timerText = GetComponent<Text>();
  }

  void Update() // 매 프레임, 값이 변경되었는지를 확인 한다
  {
    var currentTimeText = _timerComponent.CurrentTime.ToString();

    if(currentTimeText != _timerText.text)
    {
      _timerText.text = currentTimeText;
    }
  }
}

```

- UniRx의 스트림으로 구현

```C#

// 타이머측을 스트림으로 변경
public class TimerComponent : MonoBehaviour
{
  private readonly ReactiveProperty<int> _timerReactiveProperty = new IntReactiveProperty(30);

  public ReadOnlyReactiveProperty<int> CurrentTime
  {
    get { return _timerReactiveProperty.ToReadOnlyReactiveProperty(); }
  }

  private void Start()
  {
    Observable.Timer(TimeSpan.FromSeconds(1)) // 1초마다 timerReactiveProperty 값을 마이너스 한다
      .Subscribe( _ => _timerReactiveProperty.Value-- )
      .AddTo(gameObject); // 게임 오브젝트 파괴시에 자동 정지 시킨다
  }
}

// 타이머를 사용하는 측의 구현
public class TimerDisplayComponent : MonoBehaviour
{
  [SerializeField]
  private TimerComponent _timerComponent;
  private Text _timerText;

  void Start()
  {
    _timerComponent.CurrentTime // 타이머에서 값 갱신 통지가 오면, 그 타이밍에 Text를 변경하는것뿐
      .SubscribeToText(_timerText);
  }
}

```

### 스트림으로 연결하는 메리트

Observer 패턴이 간단히 구현 가능하다

- 변화를 폴링(Polling)하는 구현이 사라진다
- 필요한 타이밍에 필요한 처리를 하는 방식으로 작성하는 것이 좋다

기존의 이벤트 통지구조보다 간단

- C#의 Event는 준비단계가 귀찮아서 쓰고 싶지 않다
- Unity의 SendMessage는 쓰고 싶지 않다
- Rx라면 Observable를 준비하면 OK! 간단!

## UniRx와 UGUI를 조합하기

uGUI에서 사용할 수 있는 Model-View-00 패턴

- uGUI에 유용한 MVO패턴이 지금까지 존재하지 않았다
  -- MVC 패턴은 원래 사람에 따라서 개념이 각각 너무 다름
  -- MVVM은 데이터바인딩이 없기 때문에 사용할 수 없다

- Obervable과 ReactiveProperty를 조합하게 되면, uGUI 관련을 깔끔하게 작성가능

### Model-View-(Reactive)Presenter

MV(R)P 패턴

- Model – View – Presenter 패턴 + UniRx
- 3개의 레이어를 Observable로 심리스하게 연결

![Rx MVP패턴](https://user-images.githubusercontent.com/85855054/122228490-da9c0e80-cef2-11eb-8951-439dd4af57c8.png)

### MV(R)P패턴 만드는 법

1. Model에 ReactiveProperty를 가지게 한다
2. Presenter을 만든다
3. Presenter에 Model과 View를 등록 한다
4. Presenter내에서 View의 Observable과 Model의 ReactiveProperty 각각 Subscribe해서 연결한다

![Rx MVP 사용 예](https://user-images.githubusercontent.com/85855054/122230381-927deb80-cef4-11eb-89e0-2a38ef35e890.png)
![Rx MVP 사용 예2](https://user-images.githubusercontent.com/85855054/122230390-93af1880-cef4-11eb-8a5f-73d954d1a7b2.png)
![Rx MVP 컴포넌트 관계도](https://user-images.githubusercontent.com/85855054/122229177-7e85ba00-cef3-11eb-82d3-29b435576080.png)

### Model의 구현

```C#
public class ConfigComponent : SingletonMonoBehaviour<ConfigComponent>
{
  //ReactiveProperty를 외부에 공개
  // 읽어들이는 타이밍(ms)
  public ReactiveProperty<int> SpeechTimingReactiveProperty = new IntReactiveProperty(0);
}

```

### Presenter의 구현 (Slider 측)

```C#
using UnityEngine;
using UniRx;
using UnityEngine.UI;

public class SpeechTimingSliderPresenter : MonoBehaviour
{
  void Start()
  {
     var slider = GetComponent<Slider>();
     var config = ConfigComponent.Instance;

      // Model (ConfigComponent) -> View (slider)
     config.SpeechTimingReactiveProperty
           .SubScribe( x => slider.value = x / 10 );

      //View (slider) -> Model (ConfigComponent)
      Slider
        .OnValueChangedAsObservable()
        .DistinctUntilChanged()
        .Subscribe( x => config.SpeechTimingReactiveProperty.Value = (int)(x * 10));
  }
}

```

### Presenter의 구현 (Input 측)

```C#
using System;
using UnityEngine;
using UniRx;
using UnityEngine.UI;

public class SpeechTimingInputField : MonoBehaviour
{
  void Start()
  {
     var config = ConfigComponent.Instance;
     var inputField = GetComponent<InputField>();

      // View -> Model
    inputField.OnEndEditAsObservable()
        .Select( x => Int32.Parse(x))
        .Select( x => Mathf.Clamp(x, -1500, 1500))
        .Subscribe( x => config.SpeechTiming = x );


      // Model -> View
      config
        .SpeechTimingObservable
        .Select( x => x.ToString())
        .Subscribe( x => )
  }
}
```

### uGUI와 조합하기 정리

MV(R)P 패턴으로 uGUI 관련 설계가 편해진다

- uGUI를 사용할 경우에 아마도 현시점에서의 최적 해결법
- 프로그래머에게는 다루기 쉽지만, 비프로그래머에게는 다루기 어려워질 가능성이 있으니 주의

Presenter의 베스트한 작성 방법에 대해서는 아직 모색중

- Presenter에 어떻게 Model과 View를 등록할 것인가
- Presenter을 하나로 모을 것인가, 분할해서 만들 것인가
- 동적으로 Presenter를 생성하는 경우에는 어떻게 할 것인가
