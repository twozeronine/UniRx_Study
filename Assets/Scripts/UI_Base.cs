using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class UI_Base : MonoBehaviour
{
  protected Dictionary<Type, UnityEngine.Object[]> _objects = new Dictionary<Type, UnityEngine.Object[]>();

  public abstract void Init();

  void Start()
  {
    Init();
  }

  protected void Bind<T>(Type type, bool isRecursive = true) where T : UnityEngine.Object
  {
    // Enum 타입에서 이름을 string으로 얻어옴
    string[] names = Enum.GetNames(type);
    // Enum에서 선언한 갯수만큼 object배열을 만듬
    UnityEngine.Object[] objects = new UnityEngine.Object[names.Length];
    // 해당 배열을 딕셔너리에 넣음 나중에 Type을 통해서 배열 접근 가능.
    _objects.Add(typeof(T), objects);

    for (int i = 0; i < names.Length; i++)
    {
      if (typeof(T) == typeof(GameObject))
        objects[i] = Util.FindChild(gameObject, names[i], isRecursive ? true : false);
      else
        objects[i] = Util.FindChild<T>(gameObject, names[i], isRecursive ? true : false);

      if (objects[i] == null)
        Debug.Log($"Failed to bind({names[i]})");
    }
  }


  protected T Get<T>(int idx) where T : UnityEngine.Object
  {
    UnityEngine.Object[] objects = null;
    //딕셔너리에서 값을 꺼내옴. 
    if (_objects.TryGetValue(typeof(T), out objects) == false)
      return null;

    return objects[idx] as T;
  }

  protected GameObject GetObject(int idx) => Get<GameObject>(idx);
  protected Text GetText(int idx) => Get<Text>(idx);
  protected Button GetButton(int idx) => Get<Button>(idx);
  protected Image GetImage(int idx) => Get<Image>(idx);
}
