using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class DataStreamCanvas : MonoBehaviour {
  private const string DISPLAY_TEXT_FORMAT = "{0} msf\n({1} FPS)";
  private const string MSF_FORMAT = "#.#";
  private const float MS_PER_SEC = 1000f;

  private Text textField;
  
  public Camera cam;

  void Awake() {
    textField = GetComponent<Text>();
  }

  void Start() {
    if (cam == null) {
       cam = Camera.main;
    }

    if (cam != null) {
      // Tie this to the camera, and do not keep the local orientation.
      transform.SetParent(cam.GetComponent<Transform>(), true);
    }
  }

  void LateUpdate() {

		//textField.text = string.Format("event:%s/titem:%s/tuser:%s/tlink:%s", eve);
  }

	public void Display (string str)
	{
		textField.text = str;
	}
}
