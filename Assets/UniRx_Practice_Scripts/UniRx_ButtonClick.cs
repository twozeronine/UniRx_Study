using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;
using System;

public class UniRx_ButtonClick : UI_Base
{
  enum Buttons
  {
    Button,
    MutipleButton,
    CrossButton1,
    CrossButton2,
  }
  enum Texts
  {
    Text,
  }
  [SerializeField] Button button;
  [SerializeField] Button MutipleButton;
  [SerializeField] Button CrossButton1;
  [SerializeField] Button CrossButton2;
  [SerializeField] Text text;

  // Base.Start() => Init()
  public override void Init()
  {
    Binding();
    DoChangeText();
    DoMultipleClick();
    CrossButtonClick();
    OnDoubleClick();
  }

  // 바인딩 함수
  void Binding()
  {
    Bind<Button>(typeof(Buttons));
    Bind<Text>(typeof(Texts), false);
    button = GetButton((int)Buttons.Button);
    text = GetText((int)Texts.Text);
    MutipleButton = GetButton((int)Buttons.MutipleButton);
    CrossButton1 = GetButton((int)Buttons.CrossButton1);
    CrossButton2 = GetButton((int)Buttons.CrossButton2);
  }

  // 클릭하면 텍스트 내용을 바꿈
  void DoChangeText()
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

  // 세번클릭하는 이벤트 감지 
  void DoMultipleClick()
  {
    MutipleButton.OnClickAsObservable()
                 .Buffer(3)
                 .SubscribeToText(text, _ => "MultipleClick");
  }

  // 양쪽이 교차로 1회씩 눌릴 때 Text에 표시한다
  //// 연타하더라도 [1회 눌림]으로 판정한다
  void CrossButtonClick()
  {
    CrossButton1.OnClickAsObservable()
                .Zip(CrossButton2.OnClickAsObservable(), (b1, b2) => "CrossClicked!")
                .First() // 1번 동작한 후에 Zip내의 버퍼를 클리어 한다
                .Repeat() // Zip 내의 메시지큐를 리셋하기 위해
                .SubscribeToText(text, x => x + "\n");
  }

  void OnDoubleClick()
  {
    var clickStream = this.UpdateAsObservable()
        .Where(_ => Input.GetMouseButtonDown(0));

    clickStream.Buffer(clickStream.Throttle(TimeSpan.FromMilliseconds(200)))
        .Where(x => x.Count >= 2)
        .SubscribeToText(text, x =>
            string.Format("DoubleClick detected! \n Count:{0}", x.Count));

  }

}

