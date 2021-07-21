# MVP 패턴이란 ?

MVP 패턴에 대한 자료를 찾아보면, 정확한 정의는 없고, 대략적인 공통점이 있다.

기본적인 구조는 같지만, 세부 적인 구현 내용은 개발자의 역량에 달린 것으로 보인다.

## Model

-   Data와 관련된 모든 처리를 당담한다. 비즈니스 로직 처리
    -- 비즈니스 로직은 컴퓨터 프로그램에서 실세계의 규칙에 따라 데이터를 생성 표시 저장 변경하는 부분을 일컫는다.

## View

-   사용자에게 보여지는 UI 부분 ( 유니티에서는 모든 렌더링 되는 Object )

## Presenter

-   View에서 요청한 정보( User actions)로 Model을 가공하여 (Update model) 변경된 Model 정보를 받아 ( Model changed ) View에게 전달 ( Update UI ) 해주는 부분
-   접착제 역할

# 관계도

![유니티 MVP 패턴](https://user-images.githubusercontent.com/85855054/126413217-d144355c-b023-4746-b3cf-96908f8ecc1f.png)

## 특징

-   View와 Model은 서로를 알지 못한다. ( 어떤 방법으로든 접근할 수 없다 )
-   Presenter는 View와 Model을 알고 있다.

**여기서 알고 있다는 부분에 대한 해석은, 해당 인스턴스를 직접적으로 조작한다라고 해석해도 무방하다.**

> 직접 조작하지만 않으면 알고 있지 않은 것 ( 이벤트 방식, SendMessage 등)

## MV(R)P

UniRx 플러그인을 사용하면 유니티에서 MVP 패턴을 좀 더 쉽게 구현할 수 있다. ( MVP 패턴에서 구현해야 되는 이벤트 기반 코드들을 더 쉽게 사용 )  
원래는 Reactive Programming을 유니티에서 쉽게 사용 하기 위해 만들어진 플러그인 이다.

![유니RX MVP 패턴](https://user-images.githubusercontent.com/85855054/126413412-d81c46a0-c051-4925-a770-c298ca1e65d5.png)

MVVM 대신 MVP를 사용해야 하는 이유 ?

유니티는 UI 바인딩을 제공하지 않으며, 바인딩 레이어를 만드는 것은 복잡하며, 오버헤드가 크다.

MVP 패턴을 사용하는 Presenter는 View의 구성요소를 알고 있으며 업데이트 할 수 있다.  
실제 바인딩을 하지 않지만, View를 구독(Observable) 하여 바인딩 하는 것과 유사하게 동작하게 할 수 있다.( 단순하고, 오버 헤드도 적음)

이 패턴을 Reactive Presenter라고 한다.

```C#
// Presenter는 씬의 canvas 루트에 존재.
public class ReactivePresenter : MonoBehaviour
{
    // Presenter는 View를 알고 있다(인스펙터를 통해 바인딩 한다)
    public Button MyButton;
    public Toggle MyToggle;

    // Model의 변화는 ReactiveProperty를 통해 알 수 있다.
    Enemy enemy = new Enemy(1000);

    void Start()
    {
        // Rx는 View와 Model의 사용자 이벤트를 제공한다.
        MyButton.OnClickAsObservable().Subscribe(_ => enemy.CurrentHp.Value -= 99);
        MyToggle.OnValueChangedAsObservable().SubscribeToInteractable(MyButton);

        // Model들은 Rx를 통해 Presenter에게 자신의 변화를 알리고, Presenter은 Viw를 업데이트 한다.
        enemy.CurrentHp.SubscribeToText(MyText);
        enemy.IsDead.Where(isDead => isDead == true)
            .Subscribe(_ =>
            {
                MyToggle.interactable = MyButton.interactable = false;
            });
    }
}

// Model. 모든 프로퍼티는 값의 변경을 알려 준다. (ReactiveProperty)
public class Enemy
{
    public ReactiveProperty<long> CurrentHp { get; private set; }

    public ReactiveProperty<bool> IsDead { get; private set; }

    public Enemy(int initialHp)
    {
        // 프로퍼티 정의
        CurrentHp = new ReactiveProperty<long>(initialHp);
        IsDead = CurrentHp.Select(x => x <= 0).ToReactiveProperty();
    }
}
```

View는 하나의 Scene이며, Unity의 hierarchy라고 생각하면 된다.

View는 초기화시 Unity 엔진에 의해 Presenter와 연결된다.

XxxAsObservable 메서드를 사용하면 오버 헤드없이 이벤트 신호를 간단하게 생성 할 수 있습니다.

SusbscribeToText 및 SubscribeToInteractable은 간단한 바인딩 처럼 사용할 수 있게 하는 helper 클래스 입니다.

![유니Rx MVP패턴](https://user-images.githubusercontent.com/85855054/126415010-5613b26d-4ba6-4fb0-b84f-4810466ae285.png)

-   V -> RP -> M -> RP -> V가 완전히 Reactive한 방법으로 연결되었다.
-   GUI 프로그래밍은 ObservableTrigger의 이점도 제공합니다. ObservableTrigger는 Unity 이벤트를 Observable로 변환하므로 이를 사용하여 MV(R)P 패턴을 구성 할 수 있습니다. 예를들어 ObservableEventTrigger는 uGUI이벤트를 Observable로 변환합니다.

```C#
var eventTrigger = this.gameObject.AddComponent<ObservableEventTrigger>();
eventTrigger.OnBeginDragAsObservable()
    .SelectMany(_ => eventTrigger.OnDragAsObservable(), (start, current) => UniRx.Tuple.Create(start, current))
    .TakeUntil(eventTrigger.OnEndDragAsObservable())
    .RepeatUntilDestroy(this)
    .Subscribe(x => Debug.Log(x));

```

## 설계 방향

-   MVP 패턴을 보면서 헷갈리거나 정립되지 않는 부분은 과감히 내 방식으로 정립하고, 구현 후 문제점 발생시 개선하는 방향으로 진행.
-   완전히 디자인 패턴을 따르지는 않을 예정 (클린 코드가 되는 대신 생산성이 저하되는 부분은 과감히 생산성을 따르는 방향)
-   하나의 Presenter에 여러개의 Model이 존재할 수 있다.
    -- 각 모델의 경우 역할별로 클래스화 작업. -하나의 Presenter에 여러개의 View가 존재할 수 있다.
-   Presenter는 각 팝업, 각 오브젝트 별로 존재한다. (컴포넌트 개념으로 생각)
    -- 팝업의 아이템이 존재한다면 그 아이템도 각각의 Presenter가 존재. 구조가 복잡하지 않는다면 없어도 무방.
-   간단한 예제에서는 항상 View-Presenter-Model은 1개씩 존재 했기 때문에, 각 Presetenr 1개에 2개이상의 view와 model이 존재해도 문제 없는지에 대한 고민을 함.
-   그리고 Model의 구현시 거의 모든 역할을 Model에서 한다고 생각하면 될것으로 보임 (Presenter는 Model의 메서드를 호출하는 정도의 역할)
    -- 보통의 예제에서는 간단한 메서드 구현 정도는 Presenter에서 해주는 부분도 있지만, Model이 전부 해주는게 더 일반적인 구조인것으로 보임.

## 결론

-   MV(R)P를 사용한 설계 진행
-   MV(R)P 아키텍처에 대해 다양한 자료 조사를 진행하였음. Github에서 UniRx 개발자가 예시로든 방법이 가장 깔끔하고, 생산성 있게 구조화 할 수 있는 방법으로 판단 되었음.
-   MV(R)P 시행 착오 (현재로서는 잘못됬다고 생각하는 방법)
    -- 인터페이스를 사용하여 Presenter의 의존성을 제거하는 방법.
    -- View 컴포넌트를 따로 뺀다던가, View 자체를 여러개 둔다 던가 하는 방법.
    -- Model을 하나만 두어서 제어하는 방법.
    -- MV(R)P는 UI에만 적용하는게 더 좋겠다라고 생각한 부분
-   View의 경우 복잡해지는 경우 커스텀 View를 만드는 방식으로 해결 가능.

## 출처

[Lonpeach Tech](https://tech.lonpeach.com/2020/11/09/Thinking-about-MVRP/)
