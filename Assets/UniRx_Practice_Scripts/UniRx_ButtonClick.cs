using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class UniRx_ButtonClick : MonoBehaviour
{
  [SerializeField] private Button button;
  [SerializeField] private Text text;
  void Start() // Awake() 또는 Start()에서 Subscribe 해야하 한다. Update()에 쓰면 무수한 스트림이 생성 된다.
  {
    button.onClick
          .AsObservable()   // 이벤트를 스트림으로 변경
          .Subscribe(_ =>   // 스트림의 구독 ( 최종적으로 무엇을 할것인가를 작성 )
          {
            text.text = "Clicked";
          }); // 버튼 클릭이벤트를 스트림으로 변경해서 메세지가 도착할 때에 텍스트에 "Clicked"를 표시한다

    button.OnClickAsObservable() // UniRx에는, uGUI용의 Observable과 Subscribe가 준비되어 있다. 
          .SubscribeToText(text, _ => "Clicked"); //위와 동일한 기능
  }

}

