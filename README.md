# VRWikiViz

- The GoogleVR demo "GVRDemo" with the moving cube and floor panel disabled
- PubNub https://www.pubnub.com/developers/realtime-data-streams/wikipedia-changes/ example data stream of Wikipedia updates (PubNub Unity library requires UnityTestTools from Asset Store, not included in this repo)
- Each update creates a DataVizObject
- Which parses the html for usable image files from Wikipedia
- Display these images, plus some text, on quads moving away from the camera before stopping
- These objects collide / destroy if end up overlapping when they stop
- Is a limit on number of such objects created
- Gaze and click interactivity to select these updates, bringing in front of camera
- EasyTTS provdes text to speech for at least iOS to speak info from the Wikipedia updates
