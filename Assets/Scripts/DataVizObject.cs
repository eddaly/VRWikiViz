using System;
using System.Collections;
using UnityEngine;

public class DataVizObject: MonoBehaviour, IGvrGazeResponder
{
	private string item, user, link, country;	// The data set by Init()
	private Vector3 direction;					// The direction the object is moving in

	// The first frame the object was Start()ed
	private float firstFrame;
	public float FirstFrame {get {return firstFrame;}}

	public AudioClip audioOnGazeTrigger, audioOnDestroy;	// AudioClips set in the editor
	private AudioSource audioSource;						// Cached AudioSource component

	static private DataVizObject objectSelected = null;		// Are any DataVizObjects selected?
	static private DataVizObject objectHighlighted = null;	// Are any DataVizObjects highlighted?
	private const int maxDataVizObjects = 64;				// Maximum objects allowed (else gets too crowded)
	static private bool objectLoading = false;				// An object is loading asynchronously
	static private int allDataVizObjectCount = 0;			// Total count of DataVizObjects

	// Object status flags
//#define DESTROYONCOLLIDE
#if DESTROYONCOLLIDE
	private bool isBeingDestroyed = false;
#endif
	private bool isSelected = false;
	private bool isLoaded = false;

	private const float SelectedSpeed = 4f;		// Speed when selected
	private const float Speed = 2f;				// Regular speed moving forward
	private float speed = Speed;				// Current speed

	// Called prior to Start()
	public void Init (string _item, string _user, string _link, string _country)
	{
		item = _item;
		user = _user;
		link = _link;
		country = _country;
	}

	// Start is a coroutine to allow for asynch loading
	IEnumerator Start()
	{
		// Maximum objects allowed
		if (++allDataVizObjectCount > maxDataVizObjects) {
			DestroyObject (gameObject);
			yield break;
		}

		firstFrame = Time.time;	// Used to age objects

		// Object loading flag to prevent unhelpful queueing
		yield return new WaitWhile (() => objectLoading);
		objectLoading = true;	

		// Handy copy of audioSource
		audioSource = gameObject.GetComponent<AudioSource> ();
		Debug.Assert (audioSource);

		// Prevent object rendering while still loading
		gameObject.GetComponent<Renderer> ().enabled = false;
		TextMesh txtMesh = gameObject.GetComponentInChildren<TextMesh>();
		txtMesh.GetComponent<Renderer> ().enabled = false;

		// Load the wikipedia page
		WWW wwwHtml= new WWW (link);
		yield return wwwHtml;
		String text = wwwHtml.text;

		// Find "image" class
		int index = text.IndexOf ("class=\"image\">");
		if (index == -1) {
			Debug.Log ("No image " + link);
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
			Debug.Log ("DOWNLOADING: " + text);
			//TODO screen out GIFs?
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
			Debug.Log ("DOWNLOADING: " + text);
			//TODO screen out GIFs
			wwwHtml.Dispose ();
			wwwHtml = new WWW (text);
			yield return wwwHtml;
		}

		// Set wiki entry image as texture
		gameObject.GetComponent<Renderer>().material.mainTexture = wwwHtml.texture;
		wwwHtml.Dispose ();

		// And text as wiki update 'item'
		txtMesh.text = item;
	
		// If an object is selected, hold-on or new ones will get in the way
		yield return new WaitWhile (()=>((objectSelected != null) || (objectHighlighted != null)));

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
		//gameObject.GetComponentInChildren<Renderer>().enabled = true;
		txtMesh.GetComponent<Renderer> ().enabled = true;

		// Made it
		isLoaded = true;
		objectLoading = false;
		yield break;

		// Something went wrong getting the image, so die
no_image:
		objectLoading = false;
		DestroyObject (gameObject);
	}
		
	void Update ()
	{
		if (!isLoaded)	// Could still be loading
			return;
		
		Vector3 v = gameObject.transform.position;

		// If this object is selected, then move it toward the camera
		if (isSelected) {
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

	void OnTriggerEnter (Collider other)
	{
		return;	// Disabled this as am avoiding overlap when spawn anyway so no longer makes sense
#if DESTROYONCOLLIDE
		DataVizObject dvo = other.gameObject.GetComponent<DataVizObject> ();

		if (!isLoaded || !dvo.isLoaded)	// Could still be loading
			return;

		if (other.gameObject.name != "DataVizObject(Clone)") //yuk!
			return;

		// Unless near the end of journey, forget it
		Vector3 v = Camera.main.transform.position;
		if (Vector3.Distance (Camera.main.transform.position, transform.position) < 2.75f)
			return;

		// Destroy oldest object only
		if (firstFrame < dvo.FirstFrame) {
			if (isBeingDestroyed)
				return;
			dvo.audioSource.PlayOneShot (dvo.audioOnDestroy);
			DestroyObject (gameObject);
			isBeingDestroyed = true;
		} else {
			if (dvo.isBeingDestroyed)
				return;
			audioSource.PlayOneShot (audioOnDestroy);
			DestroyObject (dvo.gameObject);
			dvo.isBeingDestroyed = true;
		}
#endif
	}

	// Keey count of all the objects
	void OnDestroy ()
	{
		allDataVizObjectCount--;
	}

	#region IGvrGazeResponder implementation

	/// Called when the user is looking on a GameObject with this script,
	/// as long as it is set to an appropriate layer (see GvrGaze).
	public void OnGazeEnter() {

		if (!isLoaded)	// Could still be loading
			return;

		// If an object is selected, no need for more
		if (objectSelected != null)
			return;

		// Set as the highlighted object
		objectHighlighted = this;

		// But only update visually if object finished it's journey
		if (Vector3.Distance (Camera.main.transform.position, transform.position) >= 3) {
		
			// Flag selected green and display the text details
			GetComponent<Renderer> ().material.color = Color.green;
			TextMesh txtMesh = gameObject.GetComponentInChildren<TextMesh> ();
			txtMesh.text = item + "\n" + user + "\n" + link + "\n" + country;
		}
	}

	/// Called when the user stops looking on the GameObject, after OnGazeEnter
	/// was already called.
	public void OnGazeExit() {

		if (!isLoaded)	// Could still be loading
			return;
		
		// If this is selected, no need for more
		if (objectSelected == this)
			return;

		// Reset flag colour & text
		if (objectHighlighted == this) {	//TODO Not sure why necessary
			objectHighlighted = null;			
			GetComponent<Renderer> ().material.color = Color.white;
			TextMesh txtMesh = gameObject.GetComponentInChildren<TextMesh> ();
			txtMesh.text = item;
		}
	}

	/// Called when the viewer's trigger is used, between OnGazeEnter and OnPointerExit.
	public void OnGazeTrigger() {

		if (!isLoaded || objectSelected != null)	// Could still be loading, and must skip if selected
			return;

		// Remove green highlight, play the sound effect and start the select coroutine
		GetComponent<Renderer>().material.color = Color.white;
		audioSource.PlayOneShot (audioOnGazeTrigger);
		StartCoroutine (SelectObject());
	}

	// Coroutine for selecting an object (bring to front of camera, speak details)
	private IEnumerator SelectObject ()
	{
		// Switch of collision while selected
		BoxCollider collider = gameObject.GetComponent<BoxCollider> ();
		collider.enabled = false;
		isSelected = true;

		// Zip forward and back
		speed = SelectedSpeed;

		objectSelected = this;

		// Display the text details
		TextMesh txtMesh = gameObject.GetComponentInChildren<TextMesh>();
		txtMesh.text = item + "\n" + user + "\n" + link + "\n" + country;

		// Speak them
		EasyTTSUtil.SpeechFlush (item);

		// After a wait, turn back
		yield return new WaitForSeconds (4);
		txtMesh.text = item;
		isSelected = false;
		objectSelected = null;
		collider.enabled = true;
		objectHighlighted = null;	// Bit hacky but quick fix to stop holding due to this being set
	}

	#endregion

}