# VRWikiViz

- The GoogleVR demo "GVRDemo" with the moving cube and floor panel disabled
- PubNub https://www.pubnub.com/developers/realtime-data-streams/wikipedia-changes/ example data stream of Wikipedia updates
- Each update creates a DataVizObject
- Which parses the html for usable image files from Wikipedia
- Display these images, plus some text, on quads moving away from the camera before stopping
- These objects avoid overlapping
- Is a limit on number of such objects created
- Gaze and click interactivity to select these updates, bringing in front of camera
- EasyTTS provides text to speech for at least iOS to speak info from the Wikipedia updates

Tested on:
- Unity 5.5.0f3
- XCode 8.2.1
- iPhone6, iOS 10.2
- 3rd party plugins as per version in repo

Note on Wikipedia stream:
Tried to use socket.io stream https://wikitech.wikimedia.org/wiki/RCStream but this only works with v0.9 of socket.io protocol, whereas socket.io iOS implementation only available for v1. Also tried Socket.IO Unity plugin from AssetStore, which was unhappy with https URL. The pubnub stream may not be supported.
