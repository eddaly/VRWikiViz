using System;
using System.Collections;
using UnityEngine;

public class DataVizObject: MonoBehaviour, IGvrGazeResponder
{
	private string item, user, link, country;
	private Vector3 direction;
	private bool beingDestroyed = false;
	private float firstFrame;
	public float FirstFrame {get {return firstFrame;}}
	public AudioClip audioOnGazeTrigger, audioOnDestroy;
	private AudioSource audioSource;
	private bool selected = false;
	static private bool objectSelected = false;
	static private int objectCount = 0;
	private float speed = 2f;


	public void Init (string _item, string _user, string _link, string _country)
	{
		item = _item;
		user = _user;
		link = _link;
		country = _country;
	}

	IEnumerator Start()
	{
		Debug.Log ("Start called " + item + user + link + country);

		// Maximum objects allowed
		if (objectCount+1 > 128) {
			DestroyObject (gameObject);
			yield break;
		}

		firstFrame = Time.time;	// Used to age objects

		// Handy copy of audioSource
		audioSource = gameObject.GetComponent<AudioSource> ();
		Debug.Assert (audioSource);

		// Prevent object rendering while still loading
		gameObject.GetComponent<Renderer> ().enabled = false;
		gameObject.GetComponentInChildren<Renderer>().enabled = false;	//TODO This doesn't work, the text appears in it's original position during load

		// Load the wikipedia page
		WWW wwwHtml= new WWW (link);
		yield return wwwHtml;
		String text = wwwHtml.text;

		// Find "image" class
		int index = text.IndexOf ("class=\"image\">");
		if (index == -1) {
			Debug.Log ("No image " + link);	//TODO placeholder texture
			goto no_image;
		}

		// Cut off html post " class="image"
		text = text.Substring (0, index - 2);	

		//  Find the URL
		index = text.LastIndexOf ("href=\"");
		if (index == -1) {
			Debug.LogError ("No link before class=\"image\" " + link);
			goto no_image;
		}

		// Cut off html pre URL
		text = text.Substring (index + "href=\"".Length);

		// Load the File: wikipedia page for the image
		text = "https://en.wikipedia.org" + text;
		Debug.LogError (text); //TODO not an error, just separating stream
		wwwHtml.Dispose ();
		wwwHtml = new WWW (text);
		yield return wwwHtml;
		text = wwwHtml.text;

		// Ideally are multiple resolutions
		index = text.IndexOf ("mw-filepage-other-resolutions", StringComparison.OrdinalIgnoreCase); // ignore case may not be needed
		if (index == -1)
		{
			// But otherwise take the single resolution URL (which could've got from original html but seems only way to know)
			index = text.IndexOf ("fullImageLink", StringComparison.OrdinalIgnoreCase);	// ignore case may not be needed
			if (index == -1) {
				Debug.LogError ("No fullImageLink " + link);
				goto no_image;
			}

			// Cut off html pre image, "fullImageLink"
			text = text.Substring (index + "fullImageLink".Length);

			// Find src
			index = text.IndexOf ("src=");
			if (index == -1) {
				Debug.LogError ("No src for fullImageLink? " + link);
				goto no_image;
			}

			// Cut off html pre image src
			text = text.Substring (index + "src=".Length); 

			// Find end of image URL (need to skip over the opening quote)
			index = text.IndexOf ("\"", 1);
			if (index == -1) {
				Debug.LogError ("No end quote in link ?! " + link);
				goto no_image;
			}
		
			// Load the image
			text = text.Substring (1, index - 1);
			text = "https:" + text;
			Debug.LogError (text); //not an error, just separating stream
			//TODO screen out GIFs
			wwwHtml.Dispose ();
			wwwHtml = new WWW (text);
			yield return wwwHtml;
		
		}
		// We do have multiple resolutions
		else
		{
			// Cut off html pre image, "mw-filepage-other-resolutions"
			text = text.Substring (index + "mw-filepage-other-resolutions".Length);

			// End of span containing multi-res URLs
			index = text.IndexOf ("</span>");
			if (index == -1) {
				Debug.LogError ("No </span> tag after mw-filepage-other-resolutions! " + link);
				goto no_image;
			}

			// Cut off html post image, <span>
			text = text.Substring (0, index);

			// Find URL
			index = text.LastIndexOf ("<a href=");
			if (index == -1) {
				Debug.LogError ("No <a href= ? " + link);
				goto no_image;
			}

			// Cut off html pre image link
			text = text.Substring (index + "<a href=".Length);

			// Find end of image URL (need to skip over the opening quote)
			index = text.IndexOf ("\"", 1);
			if (index == -1) {
				Debug.LogError ("No end quote in link ?! " + link);
				goto no_image;
			}	

			// Load image
			text = text.Substring (1, index - 1);
			text = "https:" + text;
			Debug.LogError (text); //not an error, just separating stream
			//TODO screen out GIFs
			wwwHtml.Dispose ();
			wwwHtml = new WWW (text);
			yield return wwwHtml;
		}

		// Set wiki entry image as texture
		gameObject.GetComponent<Renderer>().material.mainTexture = wwwHtml.texture;
		wwwHtml.Dispose ();

		// And text as wiki update 'item'
		TextMesh txtMesh = gameObject.GetComponentInChildren<TextMesh>();
		txtMesh.text = item;
	
		// If an object is selected, hold-on or new ones will get in the way
		yield return new WaitWhile (()=>objectSelected);

		// Speak the item name
		EasyTTSUtil.SpeechFlush (item);

		// Set off at camera position and rotation, plus a bit
		Vector3 v = Camera.main.gameObject.transform.position;
		gameObject.transform.rotation = Camera.main.gameObject.transform.rotation;
		direction = Camera.main.transform.forward;
		v += direction * 1f;	// A unit forward so not too close
		gameObject.transform.position = v;

		// Re-enable rendering now loaded
		gameObject.GetComponent<Renderer> ().enabled = true;
		gameObject.GetComponentInChildren<Renderer>().enabled = true;

		// Made it, so up the static counter
		objectCount++;
		yield break;

		// Something went wrong getting the image, so die
no_image:
		DestroyObject (gameObject);
	}
		
	void Update ()
	{
		Vector3 v = gameObject.transform.position;

		// If this object is selected, then move it toward the camera
		if (selected) {
			if (Vector3.Distance (Camera.main.transform.position, v) > 1) {
				v -= direction * (Time.deltaTime * speed);
			}
		}
		// Otherwise, away from the camera
		else {
			if (Vector3.Distance (Camera.main.transform.position, v) < 3) {
					v += direction * (Time.deltaTime * speed);
				}
		}
		gameObject.transform.position = v;
	}

	// Not being called
	void OnTriggerEnter (Collider other)
	{
		if (other.gameObject.name != "DataVizObject(Clone)") //yuk!
			return;

		// Unless near the end of journey, forget it
		if (Vector3.Distance (Camera.main.transform.position, transform.position) < 2.75f)
			return;

		// Destroy oldest object only
		DataVizObject dvo = other.gameObject.GetComponent<DataVizObject> ();
		if (firstFrame < dvo.FirstFrame) {
			if (beingDestroyed)
				return;
			dvo.audioSource.PlayOneShot (audioOnDestroy);
			DestroyObject (gameObject);
			beingDestroyed = true;
		} else {
			if (dvo.beingDestroyed)
				return;
			audioSource.PlayOneShot (dvo.audioOnDestroy);
			DestroyObject (dvo.gameObject);
			dvo.beingDestroyed = true;
		}
	}

	void LateUpdate() {
		// Support back button means quit convention
		GvrViewer.Instance.UpdateState ();
		if (GvrViewer.Instance.BackButtonPressed) {
			Application.Quit ();
		}
	}

	// Keey count of all the objects
	void OnDestroy () {
		objectCount--;
	}

	#region IGvrGazeResponder implementation

	//TODO display close up (freezing others) on click

	/// Called when the user is looking on a GameObject with this script,
	/// as long as it is set to an appropriate layer (see GvrGaze).
	public void OnGazeEnter() {

		// Only if object finished it's journey
		if (Vector3.Distance (Camera.main.transform.position, transform.position) < 3)
			return;

		// And if nothing else selected
		if (objectSelected)
			return;	
		
		// Flag selected green and display the text details
		GetComponent<Renderer>().material.color = Color.green;
		TextMesh txtMesh = gameObject.GetComponentInChildren<TextMesh>();
		txtMesh.text = item + "\n" + user + "\n" + link + "\n" + country;
	}

	/// Called when the user stops looking on the GameObject, after OnGazeEnter
	/// was already called.
	public void OnGazeExit() {

		// Reset colour and text (may not be needed but not implemented a check)
		GetComponent<Renderer>().material.color = Color.white;
		TextMesh txtMesh = gameObject.GetComponentInChildren<TextMesh>();
		txtMesh.text = item;
	}

	/// Called when the viewer's trigger is used, between OnGazeEnter and OnPointerExit.
	public void OnGazeTrigger() {

		// If some selected already, ignore
		if (objectSelected)
			return;	

		// Play the sound effect and start the select corountine
		audioSource.PlayOneShot (audioOnGazeTrigger);
		StartCoroutine (SelectObject());
	}

	private IEnumerator SelectObject ()
	{
		// Switch of collision while selected
		BoxCollider collider = gameObject.GetComponent<BoxCollider> ();
		collider.enabled = false;
		selected = true;

		// Zip forward and back
		speed = 4;

		objectSelected = true;

		// Display the text details
		TextMesh txtMesh = gameObject.GetComponentInChildren<TextMesh>();
		txtMesh.text = item + "\n" + user + "\n" + link + "\n" + country;

		// Speak them
		EasyTTSUtil.SpeechFlush (item);

		// After a wait, turn back
		yield return new WaitForSeconds (4);
		txtMesh.text = item;
		selected = false;
		objectSelected = false;
		collider.enabled = true;
	}

	#endregion

}