## 실제 사용 예 5가지

### 1. 더블 클릭 판정

```C#

using UniRx.Trigger

var clickStream = UpdateAsObservable() // 클릭 스트림을 정의 ( 생성 아님 )
      .Where( _ => Input.GetMouseButtonDown(0));

clickStream.Buffer( clickStream.Throttle(TimeSpan.FromMilliseconds(200)))
      .Where( x => x.Count >= 2 )
      .SubscribeToText( _text, x =>
          string.Format("DoubleClick detected! \n Count: {0}", x.Count));
```

### 2. 값의 변화를 감시하기

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

### 3. 값의 변화를 가다듬기

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

### 4. WWW를 사용하기 쉽게 하기

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

### 5. 기존 라이브러리를 스트림으로 변환하기 (PhtonCloud 예제)

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
