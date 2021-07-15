# UniRx 입문 4 - Update를 스트림으로 변환하는 방법 및 장점

# Update()를 스트림으로 변환하는 방법

Unity의 Update() 호출을 스트림으로 변환하는 방법은 두 가지가 있다.

-   UniRx.Triggers의 UpdateAsObservable을 이용하는 방법
-   Observable.EveryUpdate를 이용하는 방법

## UniRx.Triggers의 UpdateAsObservable를 이용하는 방법

```C#
using UnityEngine;
using UniRx;
using UniRx.Triggers; // 이 using문이 필요

public class UpdateSample : MonoBehaviour
{
    private void Start() =>
        // UpdateAsObservable는 Component에 대한
        // 확장 메서드로 정의되어 있기 때문에 호출시
        // "this"가 필요
        this.UpdateAsObservable()
            .Subscribe(_ => Debug.Log("Update!")); // 이때 발행되는 형태는 Unit이다.
}

```

UpdateAsObservable이 GameObject가 파괴되었을때 자동으로 OnCompleted가 발행되기 때문에 스트림의 수명 관리도 쉽다.

### 구조

UpdateAsObservable은 ObservableUpdateTrigger 컴포넌트의 실체를 갖는 스트림이다.

UpdateAsObservable을 호출 하는 타이밍에 해당 GameObject에 ObservableUpdateTrigger 컴포넌트를 UniRx가 자동으로 연결하고 이 ObservableUpdateTrigger가 발행하는 이벤트를 사용하는 구조로 되어 있다.

UpdateAsObservable은 ObservableUpdateTrigger를 초기화 하는 확장 메서드

```C#
public static IObservable<Unit> UpdateAsObservable(this Component component)
{
    if (component == null || component.gameObject == null) return Observable.Empty<Unit>();
    return GetOrAddComponent<ObservableUpdateTrigger>(component.gameObject).UpdateAsObservable();
}
```

UpdateAsObservable의 본체

```C#
using System; // require keep for Windows Universal App
using UnityEngine;

namespace UniRx.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableUpdateTrigger : ObservableTriggerBase
    {
        Subject<Unit> update;

        /// <summary>Update is called every frame, if the MonoBehaviour is enabled.</summary>
        void Update()
        {
            if (update != null) update.OnNext(Unit.Default);
        }

        /// <summary>Update is called every frame, if the MonoBehaviour is enabled.</summary>
        public IObservable<Unit> UpdateAsObservable()
        {
            return update ?? (update = new Subject<Unit>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (update != null)
            {
                update.OnCompleted();
            }
        }
    }
}
```

![2020-02-23-16](https://user-images.githubusercontent.com/85855054/125711782-5cb1cf4e-bd61-4303-a80e-96aaa40dee25.png)

이처럼 UpdateAsObservable을 호출하는 것으로 ObservableUpdateTrigger라는 컴포넌트를 해당 GameObject에 붙이고,  
ObservableUpdateTrigger에서 실행되는 Update()를 내부에 가지는 Subject를 사용하여 단지 이벤트를 발행하는 구조고 되어있다.

주의 할점은 두 가지가 있다.

1. ObservableUpdateTrigger라는 컴포넌트가 갑자기 증가하더라도 그것은 정상 동작이므로 삭제하지 말자
2. 1개의 GameObject마다 1개의 ObservableUpdateTrigger를 공유하고 있기 때문에 UpdateAsObservable 자체를 무수히 Subscribe해도 그다지 비용 증가는 없다.
