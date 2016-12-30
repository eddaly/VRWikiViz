using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PubNubMessaging.Core;

public class main : MonoBehaviour {

	Pubnub pubnub;
	public DataStreamCanvas dataStreamCanvas;
	bool connected = false;
	GameObject myDataVizObjectPrefab;

	// Use this for initialization
	void Start () {

		if (dataStreamCanvas == null) {
			Debug.LogError ("dataSteamCanvas not initialised in Editor");
		}
		pubnub = new Pubnub ("demo", "sub-c-b0d14910-0601-11e4-b703-02ee2ddab7fe", "", "", true);

		myDataVizObjectPrefab = Resources.Load ("DataVizObject") as GameObject;

		EasyTTSUtil.Initialize (EasyTTSUtil.UnitedKingdom);

		//StartCoroutine (MakeDataVizTestObjects ());
	}

	void OnApplicationQuit() 
	{
		EasyTTSUtil.Stop ();
	}
	
	// Update is called once per frame
	void Update () {
		if (!connected && pubnub != null) {
			pubnub.Subscribe<string>(
				"pubnub-wikipedia", 
				DisplaySubscribeReturnMessage, 
				DisplaySubscribeConnectStatusMessage, 
				DisplayErrorMessage); 
			connected = true;
		}
	}

	void DisplaySubscribeConnectStatusMessage(string connectMessage)
	{
		UnityEngine.Debug.Log("SUBSCRIBE CONNECT CALLBACK "+ connectMessage);
		connected = true;
	}

	void DisplaySubscribeReturnMessage(string result)
	{
		UnityEngine.Debug.Log("SUBSCRIBE REGULAR CALLBACK: " + result); 

		// Check valid message
		if (!string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(result.Trim()))
		{
			List<object> deserializedMessage = pubnub.JsonPluggableLibrary.DeserializeToListOfObject(result);

			if (deserializedMessage != null && deserializedMessage.Count > 0)
			{
				object subscribedObject = (object)deserializedMessage[0];
				if (subscribedObject != null)
				{
					string resultActualMessage = pubnub.JsonPluggableLibrary.SerializeToJsonString(subscribedObject);

					Dictionary<string, object> dict = (Dictionary<string, object>)subscribedObject;

					// Check expected message
					if (dict.ContainsKey ("event")) {
						if (dict ["event"].ToString() == "wiki modification") {
							if (dict.ContainsKey ("item") && dict.ContainsKey ("user") && dict.ContainsKey ("link")) {
								if (!dict.ContainsKey ("country")) {
									Debug.LogError ("No 'country' data");
									}

								// All well
								MakeDataVizObject (dict);
								return;
							}
						}
					}
					// Unexpected message
					Debug.LogError ("Unexpected message: " + resultActualMessage);
					return;
				}
			}
		}
		Debug.LogError ("Invalid message: " + result);
	}

	IEnumerator MakeDataVizTestObjects ()
	{
		Dictionary<string, object> dict = new Dictionary<string, object> ();

		dict.Add ("item", "User:Gjrun");
		dict.Add ("user", "Gjrun");
		dict.Add ("link", "https://en.wikipedia.org/w/index.php?oldid=756878020&rcid=895604328");
		dict.Add ("country", "#en.wikipedia");
		MakeDataVizObject (dict);
		dict.Clear ();

		yield return new WaitForSeconds (3);

		dict.Add ("item", "Talk:Montague North");
		dict.Add ("user", "Johnsoniensis");
		dict.Add ("link", "https://en.wikipedia.org/w/index.php?diff=756878021&oldid=747171299");
		dict.Add ("country", "#en.wikipedia");
		MakeDataVizObject (dict);
		dict.Clear ();

		yield return new WaitForSeconds (3);

		dict.Add ("item", "Trainworks Railway Museum");
		dict.Add ("user", "121.44.254.176");
		dict.Add ("link", "https://en.wikipedia.org/w/index.php?diff=756878024&oldid=754138554");
		dict.Add ("country", "#en.wikipedia");
		MakeDataVizObject (dict);
		dict.Clear ();
	}

	void MakeDataVizObject (Dictionary<string, object> dict)
	{
		GameObject gameObject = Instantiate (myDataVizObjectPrefab);
		DataVizObject dataVizObject = gameObject.GetComponent<DataVizObject>();
		dataVizObject.Init (dict ["item"].ToString (), dict ["user"].ToString (), dict ["link"].ToString (), dict ["country"].ToString ());
		//dataStreamCanvas.Display (dict ["item"].ToString ());
	}

	void DisplayErrorMessage(PubnubClientError pubnubError)
	{
		UnityEngine.Debug.Log("ERROR MESSAGE CALLBACK:"); 
		UnityEngine.Debug.Log(pubnubError.StatusCode);

		//Based on the severity of the error, we can filter out errors for handling or logging.
		switch (pubnubError.Severity)
		{
		case PubnubErrorSeverity.Critical:
			//This type of error needs to be handled.
			break;
		case PubnubErrorSeverity.Warn:
			//This type of error needs to be handled
			break;
		case PubnubErrorSeverity.Info:
			//This type of error can be ignored
			break;
		default:
			break;
		}

		//TODO cope with 122 and 400 (no connection) errors

		UnityEngine.Debug.Log(pubnubError.StatusCode); //Unique ID of the error

		UnityEngine.Debug.Log(pubnubError.Message); //Message received from client or server. From client, it could be from .NET exception.

		if (pubnubError.DetailedDotNetException != null)
		{
			UnityEngine.Debug.Log(pubnubError.IsDotNetException); // Boolean flag to check .NET exception
			UnityEngine.Debug.Log(pubnubError.DetailedDotNetException.ToString()); // Full Details of .NET exception
		}

		UnityEngine.Debug.Log(pubnubError.MessageSource); // Did this originate from Server or Client-side logic

		if (pubnubError.PubnubWebRequest != null)
		{
			//Captured Web Request details
			UnityEngine.Debug.Log(pubnubError.PubnubWebRequest.RequestUri.ToString()); 
			UnityEngine.Debug.Log(pubnubError.PubnubWebRequest.Headers.ToString()); 
		}

		if (pubnubError.PubnubWebResponse != null)
		{
			//Captured Web Response details
			UnityEngine.Debug.Log(pubnubError.PubnubWebResponse.Headers.ToString());
		}

		UnityEngine.Debug.Log(pubnubError.Description); // Useful for logging and troubleshooting and support
		UnityEngine.Debug.Log(pubnubError.Channel); //Channel name(s) at the time of error
		UnityEngine.Debug.Log(pubnubError.ErrorDateTimeGMT); //GMT time of error
	}

	void DisplayReturnMessage(string result)
	{
		UnityEngine.Debug.Log("PUBLISH STATUS CALLBACK");
		UnityEngine.Debug.Log(result);
	}
}
