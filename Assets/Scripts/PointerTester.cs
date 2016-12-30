using UnityEngine;
using UnityEngine.EventSystems;

public class PointerTester : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {

	// Use this for initialization
	public void OnPointerEnter(PointerEventData eventData)
	{
		DataVizObject dvo = gameObject.GetComponent<DataVizObject> ();
		dvo.OnGazeEnter ();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		DataVizObject dvo = gameObject.GetComponent<DataVizObject> ();
		dvo.OnGazeExit ();
	}

	public void OnPointerClick (PointerEventData eventData)
	{
		DataVizObject dvo = gameObject.GetComponent<DataVizObject> ();
		dvo.OnGazeTrigger ();
	}
}
