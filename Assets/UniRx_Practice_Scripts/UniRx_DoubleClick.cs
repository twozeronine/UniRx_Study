using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using UnityEngine.UI;

public class UniRx_DoubleClick : MonoBehaviour
{
  [SerializeField] private Button button;
  [SerializeField] private Text text;
  void Start()
  {
    // 매프레임 마우스 클릭을 관찰할 수 있게 스트림으로 변환.
    var doubleClickStream = Observable.EveryUpdate()
                          .Where(_ => Input.GetMouseButtonDown(0));

    //
  }

}
