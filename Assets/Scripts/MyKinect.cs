using UnityEngine;
using System;
using System.IO;
using freenect;

public class MyKinect : MonoBehaviour
{

	private static int TEXTURE_SIZE = 1024;

	private Kinect kinect;
	private int kinectWidth;
	private int kinectHeight;

	private int terrainWidth;
	private int terrainHeight;

	public Texture2D depthTexture;
	public GameObject terrain; //this is actually a 'Plane' object
	public Material terrainMaterial;
	public Shader terrainMaterialShader;

	void Awake ()
	{
		Debug.Log ("Slowing overall framerate");
		Application.targetFrameRate = 5;
	}

	// Use this for initialization
	void Start ()
	{
		if (freenect.Kinect.DeviceCount == 1) {
			Debug.Log ("Initialising kinect");
			kinect = new Kinect (0);
			kinect.Open ();

			kinect.DepthCamera.DataReceived += HandleKinectDepthCameraDataReceived;
			kinect.DepthCamera.Mode = kinect.DepthCamera.Modes [5]; // use mm depth format

			kinect.DepthCamera.Start ();
			kinect.LED.Color = LEDColor.Red;

			kinectWidth = kinect.DepthCamera.Mode.Width;
			kinectHeight = kinect.DepthCamera.Mode.Height;

			Debug.Log ("Started kinect with mode " + kinect.DepthCamera.Mode);
			Debug.Log ("kinect frame data size " + kinect.DepthCamera.Mode.Size);
			Debug.Log ("kinect data shape : Height [" + kinectHeight + "], Width[" + kinectWidth + "]");
			Debug.Log ("kinect mode databitsperpixel " + kinect.DepthCamera.Mode.DataBitsPerPixel);
			Debug.Log ("kinect mode paddingbitsperpixel " + kinect.DepthCamera.Mode.PaddingBitsPerPixel);

			depthTexture = new Texture2D (kinectWidth, kinectHeight, TextureFormat.RGB565, false);

			terrain = GameObject.Find ("Terrain");
			terrainWidth = (kinectWidth /10) - 1;
			terrainHeight = (kinectHeight /10) - 1;
			terrain.transform.localScale = new Vector3 (terrainWidth, 1, terrainHeight);

			terrainMaterial = terrain.GetComponent<Renderer>().material;
			terrainMaterial.SetFloat ("_DepthTexWidth", kinectWidth);
			terrainMaterial.SetFloat ("_DepthTexHeight", kinectHeight);
			terrainMaterialShader = terrainMaterial.shader;

		} else {
			throw new Exception ("Could not initialise kinect as no devices were found.");
		}
	}

	// Update is called once per frame
	void Update ()
	{
		// Update status of accelerometer/motor etc.
		kinect.UpdateStatus ();
		// Process any pending events.
		Kinect.ProcessEvents ();
	}
		
	// Callback for kinect camera, called when depth data stream is received
	private void HandleKinectDepthCameraDataReceived (object sender, BaseCamera.DataReceivedEventArgs e)
	{
//		byte[] kinectRawData = e.Data.Data;
//		ushort[] kinectDepthValues = new ushort[320 * 240];
//		for (int i = 0; i < kinectDepthValues.Length; i ++) {
//			ushort s = BitConverter.ToUInt16 (e.Data.Data, i*2);
//			kinectDepthValues [i] = s;
//		}

//		Debug.Log(BitConverter.IsLittleEndian);
//		Debug.Log(BitConverter.ToUInt16 (e.Data.Data, 0));
		depthTexture.LoadRawTextureData (e.Data.Data);
		depthTexture.Apply();
//		terrainMaterial.SetTexture ("_Heightmap", depthTexture);
	}

	//Used to tidy up when application is exited
	void OnApplicationQuit ()
	{
		Debug.Log ("Application ending after " + Time.time + " seconds, closing kinect gracefully.");
		kinect.LED.Color = LEDColor.BlinkGreen;
		kinect.Close ();
	}
		
}
