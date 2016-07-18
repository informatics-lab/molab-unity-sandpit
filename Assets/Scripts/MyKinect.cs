using UnityEngine;
using System;
using System.IO;
using freenect;

public class MyKinect : MonoBehaviour
{
	private Kinect kinect;
	private int kinectWidth;
	private int kinectHeight;

	private int terrainWidth;
	private int terrainHeight;

//	private static ushort MAX_KINECT_VALUE = 1050;
//	private static ushort MIN_KINECT_VALUE = 100;
//	public static ushort MAX_KINECT_VALUE = 670;
//	public static ushort MIN_KINECT_VALUE = 415;
	public static ushort MAX_KINECT_VALUE = 610;
	public static ushort MIN_KINECT_VALUE = 415;

	private static int ypxl = 480;
	private static int xpxl = 640;

	public Texture2D depthTexture;

	public GameObject terrain1; //this is actually a 'Plane' object
	public GameObject terrain2;
	public GameObject terrain3;
	public GameObject terrain4;
	public GameObject terrain5;
	public GameObject terrain6;

	public Material terrain1Material;
	public Material terrain2Material;
	public Material terrain3Material;
	public Material terrain4Material;
	public Material terrain5Material;
	public Material terrain6Material;

	public Shader terrainMaterialShader;
	public RenderTexture renderTexture;

//	private ushort[,] filterCollection;
//	private short [,] filterSquare;
//	private uint index_filterSquare;
//
//	private ushort [] KinectSA  = new ushort  [640 * 480];
//	private byte   [] KinectBA  = new byte    [640 * 480 * 2];
//	private int counter = 0;
//	private int mn = 99999;
//	private int mx = 0;
//
//	private int badVal      = 0;
//	private int startsample = 1;
//	private int stopsample  = 1 + 50;

	public Camera MainCam;

	public float moveSpeed = 50.0f;
	private float xMove = 1.0f;
	private float zMove = 1.0f;
	private float yMove = 1.0f;



	//============//============//============//============//============//============//

	void Awake ()
	{
		Debug.Log ("Slowing overall framerate");
	}

	// Use this for initialization
	void Start ()
	{
		if (freenect.Kinect.DeviceCount == 1) {
			Debug.Log ("Initialising kinect");
			kinect = new Kinect (0);
			kinect.Open ();

			kinect.DepthCamera.DataReceived += HandleKinectDepthCameraDataReceived;

			kinect.DepthCamera.Start ();
			kinect.LED.Color = LEDColor.Red;

			kinectWidth = kinect.DepthCamera.Mode.Width;
			kinectHeight = kinect.DepthCamera.Mode.Height;

			Debug.Log ("Started kinect with mode " + kinect.DepthCamera.Mode);
			Debug.Log ("kinect frame data size " + kinect.DepthCamera.Mode.Size);
			Debug.Log ("kinect data shape : Height [" + kinectHeight + "], Width[" + kinectWidth + "]");
			Debug.Log ("kinect mode databitsperpixel " + kinect.DepthCamera.Mode.DataBitsPerPixel);
			Debug.Log ("kinect mode paddingbitsperpixel " + kinect.DepthCamera.Mode.PaddingBitsPerPixel);

			depthTexture = new Texture2D (kinectWidth, kinectHeight, TextureFormat.ARGB32, false);

			terrain1Material = terrain1.GetComponent<Renderer>().material;
			terrain2Material = terrain2.GetComponent<Renderer>().material;
			terrain3Material = terrain3.GetComponent<Renderer>().material;
			terrain4Material = terrain4.GetComponent<Renderer>().material;
			terrain5Material = terrain5.GetComponent<Renderer>().material;
			terrain6Material = terrain6.GetComponent<Renderer>().material;

			terrain1Material.SetInt ("_Grid_x_Index", 0);
			terrain1Material.SetInt ("_Grid_z_Index", 0);
			terrain2Material.SetInt ("_Grid_x_Index", 1);
			terrain2Material.SetInt ("_Grid_z_Index", 0);
			terrain3Material.SetInt ("_Grid_x_Index", 2);
			terrain3Material.SetInt ("_Grid_z_Index", 0);
			terrain4Material.SetInt ("_Grid_x_Index", 0);
			terrain4Material.SetInt ("_Grid_z_Index", 1);
			terrain5Material.SetInt ("_Grid_x_Index", 1);
			terrain5Material.SetInt ("_Grid_z_Index", 1);
			terrain6Material.SetInt ("_Grid_x_Index", 2);
			terrain6Material.SetInt ("_Grid_z_Index", 1);

			// Set camera position
			Camera.main.transform.localPosition = new Vector3 (100, 550, 0);
			Camera.main.transform.localRotation = Quaternion.Euler (90, 0, 0);
		
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



		// Dynamic Camera movement with arrow keys
		xMove = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;

		if (Input.GetKey (KeyCode.LeftShift)) {
			yMove = Input.GetAxis ("Vertical") * moveSpeed * Time.deltaTime;
		} else {
			zMove = Input.GetAxis ("Vertical") * moveSpeed * Time.deltaTime;
		}
			
		Camera.main.transform.Translate (xMove, zMove, yMove);


	}
		
	// Callback for kinect camera, called when depth data stream is received
	private void HandleKinectDepthCameraDataReceived (object sender, BaseCamera. DataReceivedEventArgs e)
	{
		// initialise empty color object
		Color32[] colorsSM  = new Color32 [kinectWidth * kinectHeight];

			// convert the byte array into a color32 texture and set as depth texture for the shader.
			// loop through the ushort array and replace elements with average.
			for (int i = 0; i < colorsSM.Length; i++) {	

				ushort s = BitConverter.ToUInt16 (e.Data.Data, i * 2);	//value of pixel

				// Reset the pixel if its value lies outside the bounds of MAX_KINECT_VALUE
				// and MIN_KINECT_VALUE + something. 
				// Any pixel returing the 2047 error value is set to MIN_KINECT_VALUE
				// this unique value can then be picked up in the shader.
				if (s > MAX_KINECT_VALUE && s != 2047) {s = MAX_KINECT_VALUE ;}
				if (s <= MIN_KINECT_VALUE) {s = (ushort) ( (int) MIN_KINECT_VALUE + 50) ;}

				if (s < MIN_KINECT_VALUE + 50) {s = MIN_KINECT_VALUE;} 

				Color32 color = new Color32 ();	// Take the colour, scale it and put it into a colour array.
				double ScalingFactor = 255.0 / (MAX_KINECT_VALUE - MIN_KINECT_VALUE);
				uint scaled = (uint) Math.Ceiling ((s - MIN_KINECT_VALUE) * ScalingFactor); 
				color.a = 0;
				color.b = 0;
				color.g = 0;
				color.r = (byte) (scaled);
				colorsSM [i] = color;

			} // ENDFOR

			depthTexture.SetPixels32 (colorsSM);
			depthTexture.Apply();

			terrain1Material.SetTexture ("_DepthTex", depthTexture);
			terrain2Material.SetTexture ("_DepthTex", depthTexture);
			terrain3Material.SetTexture ("_DepthTex", depthTexture);
			terrain4Material.SetTexture ("_DepthTex", depthTexture);
			terrain5Material.SetTexture ("_DepthTex", depthTexture);
			terrain6Material.SetTexture ("_DepthTex", depthTexture);

	} // ENDFUNCTION



//	Color32 [] SetFilterSquare( Color32 [] ColorsSM, byte [] KinectBA ) {
//			// Also preps data for filtering
//
//			//looping over each integer(pixel value in the array)
//			// i is the index of that value in the byte array
//			for (int i = 0; i < ColorsSM.Length; i++) {
//
//				// value of pixel
//				ushort s = BitConverter.ToUInt16 (KinectBA, i * 2);
//
//				// if the value of that integer is 2047 i.e. the returned error value
//				if (s == 0) {
//					s = 255;
//				}
//
//					// size of grid to look at filtering
//					int filterGrid = (int) Math.Pow (4, 2);
//					// define the bounds of the surrounding pixels
//					//int minus = (int) (0 - ((Math.Sqrt (filterGrid) / 2) - 1));
//					//int plus = (int)  (    ((Math.Sqrt (filterGrid) / 2) + 1));
//
//					// initialise empty table to put info into
//					filterSquare = new short[filterGrid - 1, 4];
//					index_filterSquare = 0;
//
//					// the indeces of the 2047 pixel in the 2d image
//					// ypxl == 480 (down), xpxl == 640 (across)
//					// Definitely correct
//					var ImageColumnIndex = i % ypxl; 
//					var ImageRowIndex    = (i - ImageColumnIndex) / xpxl;
//
//					// scan every pixel in the filter grid
//					// for all pixels in the filter square
//					for (int yi = -3; yi < 2; yi++) {
//						for (int xi = -3; xi < 2; xi++) {
//							// (ignoring the one we are looking at)
//							if (xi != 0 || yi != 0) {
//
//								// This is the adjacent pixel indeces
//								int ImageColumnIndexADJ = ImageColumnIndex + yi;
//								int ImageRowIndexADJ    = ImageRowIndex    + xi;
//
//								// only consider where the newly calculated indeces exist 
//								// (i.e. not greater than the bounds of the image)
//								if (ImageColumnIndexADJ >= 0 && ImageColumnIndexADJ <= (xpxl - 1) &&
//									ImageRowIndexADJ    >= 0 && ImageRowIndexADJ    <= (ypxl - 1)) {
//
//									//what is the index in the byte array that corresponds to 
//									// the newly calculated adjacent pixel indeces?
//									var BAequivI = (ImageRowIndexADJ * ypxl) + ImageColumnIndexADJ;
//									var val = BitConverter.ToUInt16 (KinectBA, BAequivI * 2);
//
//									// Populate filterSquare table (array) with image indeces, byte array index and value.
//									filterSquare [index_filterSquare, 0] = (short) ImageColumnIndexADJ; //ImageColumnIndex
//									filterSquare [index_filterSquare, 1] = (short) ImageRowIndexADJ; //ImageRowIndex
//									filterSquare [index_filterSquare, 2] = (short) BAequivI; //ByteArrayIndex
//									filterSquare [index_filterSquare, 3] = (short) val; //value at that Byte Array equivalent index
//
//									index_filterSquare += 1;
//
//								}//ENDIF						
//							}//ENDIF
//						}//ENDFOR
//					}//ENDFOR
//
//				// APPLY FILTER
//				s = FilterVal (filterSquare, index_filterSquare);
//
//				int total = 0;
//				for (int n = 0; n < index_filterSquare; n++) {
//					if (filterSquare [n, 3] < badVal) {
//						
//						total += filterSquare [n, 3];
//					}
//				}
//
//				s = (ushort) total;
//
//
//				}//ENDIF
//				
//
//
//		}//ENDFOR
//
//		return ColorsSM;
//
//	} // ENDFUNCTION



//	ushort FilterVal (short [,] FSarray, uint FSlength){
//		// FSarray = filter square array with:
//		// . Indeces of the pixel (image space) [n, 0] and [n, 1].
//		// . Index in the byte array [n, 2].
//		// . Value in the byte array/of that pixel [n, 3].
//		// FSlength = length of the filter square, initialised before to a max of 24 but can be shorter
//		// if on an edge etc..
//
//		// Initialise a table that will hold the values and frequencies of 
//		// the pixel values from the filter grid.
//		ushort [,] filterCollection = new ushort[FSlength, 2];
//
//		uint N2047s = 0;
//		// go through every row in the filterSquare table 
//		for (uint i = 0; i < FSlength; i++){
//			
//			// count how many null points there are
//			//if (FSarray [i, 3] == badVal) { N2047s += 1; }// ENDIF
//
//			// Scan through all entries and add 1 to the freq if a previously detected depth is detected
//			for (uint k = 0; k <= i; k ++) {
//				if (filterCollection [k, 0] == (ushort) FSarray [i, 3]) { 
//					filterCollection [i, 1] += 1;
//				}// ENDIF
//			} // ENDFOR
//
//			if (filterCollection [i, 0] == 0) {				
//				filterCollection [i, 0] = (ushort) FSarray [i, 3];
//				filterCollection [i, 1] += 1;
//			} // ENDIF
//
//		}//ENDFOR
//
//		// Should now have a filter collection table (array) of values and 
//		// frequencies from the FilterSquare table and a count of the number of bad pixels (N2047s)
//		// want something to say if all values in filter collection are 2047 then null the point
//
//	// Assign returned value
//		int svalue = 0;
//		//int?
//		ushort maxFreq = 0;
//		int jval = 0;
//		for (int j = 0; j < FSlength; j++){
//			//if ( filterCollection[j, 1] > maxFreq){
//				maxFreq = filterCollection [(int) j, 1];
//				//svalue = filterCollection [(int) j, 0] * filterCollection [(int) j, 1] ;
//				svalue += filterCollection [(int) j, 0] * filterCollection [(int) j, 1] ;
//				jval = j;
//			//}//ENDIF
//		
//			if (jval != 0) {
//				svalue =  svalue / jval;
//			}
//
//		}//ENDFOR
//
//		return (ushort) svalue;
//
//	}//ENDFUNCTION




	//Used to tidy up when application is exited
	void OnApplicationQuit ()
	{
		Debug.Log ("Application ending after " + Time.time + " seconds, closing kinect gracefully.");
		kinect.LED.Color = LEDColor.BlinkGreen;
		kinect.Close ();
	}
		
}
