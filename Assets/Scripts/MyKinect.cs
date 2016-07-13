using UnityEngine;
using System;
using System.IO;
using freenect;

public class MyKinect : MonoBehaviour
{

//	private static int TEXTURE_SIZE = 1024;

	private Kinect kinect;
	private int kinectWidth;
	private int kinectHeight;

	private int terrainWidth;
	private int terrainHeight;

//	private static ushort MAX_KINECT_VALUE = 1050;
//	private static ushort MIN_KINECT_VALUE = 100;
	private static ushort MAX_KINECT_VALUE = 620;
	private static ushort MIN_KINECT_VALUE = 500;
	private static int ypxl = 480;
	private static int xpxl = 640;

	public Texture2D depthTexture;

	public GameObject terrain; //this is actually a 'Plane' object
	public Material terrainMaterial;
	public Shader terrainMaterialShader;
	public RenderTexture renderTexture;

	private ushort[,] filterCollection;
	private short [,] filterSquare;
	private uint index_filterSquare;

	private ushort [] KinectSA  = new ushort  [640 * 480];
	private byte   [] KinectBA  = new byte    [640 * 480 * 2];
	private int counter = 0;


	//============//============//============//============//============//============//

	void Awake ()
	{
		Debug.Log ("Slowing overall framerate");
		//Application.targetFrameRate = 1;
		//QualitySettings.vSyncCount = 0;

	}

	// Use this for initialization
	void Start ()
	{
		if (freenect.Kinect.DeviceCount == 1) {
			Debug.Log ("Initialising kinect");
			kinect = new Kinect (0);
			kinect.Open ();

			kinect.DepthCamera.DataReceived += HandleKinectDepthCameraDataReceived;
			//kinect.DepthCamera.Mode = kinect.DepthCamera.Modes [5];

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
			//renderTexture = new RenderTexture (kinectWidth, kinectHeight, 0, RenderTextureFormat.R,RenderTextureReadWrite.Linear);

			terrain = GameObject.Find ("Terrain");
			terrainWidth  = (kinectWidth  / 10) - 1;
			terrainHeight = (kinectHeight / 10) - 1;
			terrain.transform.localScale = new Vector3 (terrainWidth, 1, terrainHeight);

			terrainMaterial = terrain.GetComponent<Renderer>().material;
			terrainMaterial.SetFloat ("_DepthTexWidth", kinectWidth);
			terrainMaterial.SetFloat ("_DepthTexHeight", kinectHeight);
			terrainMaterialShader = terrainMaterial.shader;

			terrainMaterial.SetFloat ("_DepthTexHeight", kinectHeight);
			terrainMaterial.SetInt ("_MAX_KINECT_VALUE", MAX_KINECT_VALUE);
			terrainMaterial.SetInt ("_MIN_KINECT_VALUE", MIN_KINECT_VALUE);

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
	private void HandleKinectDepthCameraDataReceived (object sender, BaseCamera. DataReceivedEventArgs e)
	{

		// initialise empty color object
		Color32[] colorsSM  = new Color32 [kinectWidth * kinectHeight];
		Debug.Log ("counter :" + counter);

		// counter is set as global variable = 0.
		if (counter != 40 || counter % 2 != 0) {

			// loop through the byte array and convert elements to ushort to put in ushort array
			//Add together the elements from each iteration until the if statment condition is met.
			for (int i = 0; i < colorsSM.Length; i++) {
				KinectSA [i]  +=  BitConverter.ToUInt16(e.Data.Data, i * 2);
			} 
			// ENDFOR
			counter += 1;

		} else {

			// loop through the ushort array and replace elements with average.
			for (int i = 0; i < colorsSM.Length; i++) {	

				// Divide by number of times the images were added together (to get mean)
				KinectSA [i] = (ushort) ( ( (int) KinectSA [i] ) / (counter - 1) );
				// Convert back to byte Array
				KinectBA [i * 2] = (byte) KinectSA [i];

			} // ENDFOR

			// restart the counter 
			counter = 0;

			// Flip axis to correct for mirroring image
			//KinectBA = FlipImage (KinectBA);
			// convert the byte array into a color32 texture and set as depth texture for the shader.
			colorsSM = Byte2Color (colorsSM, KinectBA);
			depthTexture.SetPixels32 (colorsSM);
			depthTexture.filterMode = FilterMode.Bilinear;
			depthTexture.Apply();
			terrainMaterial.SetTexture ("_DepthTex", depthTexture);

		} // ENDELSEIF

	} // ENDFUNCTION



	Color32 [] Byte2Color( Color32 [] ColorsSM, byte [] KinectBA ) {

			//looping over each integer(pixel value in the array)
			// i is the index of that value in the byte array
			for (int i = 0; i < ColorsSM.Length; i++) {

				// value of pixel
				ushort s = BitConverter.ToUInt16 (KinectBA, i * 2);

				// if the value of that integer is 2047 i.e. the returned error value
				if (s <= 100 || s >= 2047) {

					// size of grid to look at filtering
					int filterGrid = (int) Math.Pow (4, 2);
					// define the bounds of the surrounding pixels
					int minus = (int) (0 - ((Math.Sqrt (filterGrid) / 2) - 1));
					int plus = (int)  (    ((Math.Sqrt (filterGrid) / 2) + 1));

					// initialise empty table to put info into
					filterSquare = new short[filterGrid - 1, 4];
					index_filterSquare = 0;

					// the indeces of the 2047 pixel in the 2d image
					// ypxl == 480 (down), xpxl == 640 (across)
					// Definitely correct
					var ImageColumnIndex = i % ypxl; 
					var ImageRowIndex = (i - ImageColumnIndex) / xpxl;

					// scan every pixel in the filter grid
					// for all pixels in the filter square
					for (int yi = minus; yi < plus; yi++) {
						for (int xi = minus; xi < plus; xi++) {
							// (ignoring the one we are looking at)
							if (xi != 0 || yi != 0) {

								// This is the adjacent pixel indeces
								var ImageColumnIndexADJ = ImageColumnIndex + yi;
								var ImageRowIndexADJ    = ImageRowIndex    + xi;

								// only consider where the newly calculated indeces exist 
								// (i.e. not greater than the bounds of the image)
								if (ImageColumnIndexADJ >= 0 && ImageColumnIndexADJ <= (xpxl - 1) &&
									ImageRowIndexADJ    >= 0 && ImageRowIndexADJ    <= (ypxl - 1)) {

									//what is the index in the byte array that corresponds to 
									// the newly calculated adjacent pixel indeces?
									var BAequivI = (ImageRowIndexADJ * ypxl) + ImageColumnIndexADJ;
									var val = BitConverter.ToUInt16 (KinectBA, BAequivI * 2);

									// Populate filterSquare table (array) with image indeces, byte array index and value.
									filterSquare [index_filterSquare, 0] = (short)ImageColumnIndexADJ; //ImageColumnIndex
									filterSquare [index_filterSquare, 1] = (short)ImageRowIndexADJ; //ImageRowIndex
									filterSquare [index_filterSquare, 2] = (short)BAequivI; //ByteArrayIndex
									filterSquare [index_filterSquare, 3] = (short)val; //value at that Byte Array equivalent index

									index_filterSquare += 1;

								}//ENDIF						
							}//ENDIF
						}//ENDFOR
					}//ENDFOR

					// APPLY FILTER
					//s = FilterVal (filterSquare, index_filterSquare);

				}//ENDIF
				
		// Take the 
		Color32 color = new Color32 ();
		double ScalingFactor = 255.0 / (MAX_KINECT_VALUE - MIN_KINECT_VALUE);
		uint scaled = (uint) Math.Ceiling ((s - MIN_KINECT_VALUE) * ScalingFactor); 
		color.a = 255;
		color.b = 0;
		color.g = 0;
		color.r = (byte) (scaled);
		ColorsSM [i] = color;

		// rehash the color object with the smoothed value in the red channel.
		color.r = (byte) scaled;
		ColorsSM [i] = color;

		}//ENDFOR

		return ColorsSM;

	} // ENDFUNCTION




	byte [] FlipImage (byte [] ByteArray){

		// Initialise empty byte arrays to take
		// The completely reversed image
		// The rows subset to reverse iteratively
		//byte [] RevdByteArray = new byte [ByteArray.Length];
		//byte [] ByteRow       = new byte [kinectWidth * 2]; 

		//Debug.Log ("ByteArray [319] : " + ByteArray [319]);

		// For each row of the image
		//for ( int i = 0; i < ByteArray.Length; i = (i * kinectHeight * 2) ) {
		
			// Copy the image row into a new array
			// Source array, start index, destination array, start index, length)
			//Array.Copy(ByteArray, (i * kinectHeight * 2), ByteRow, 0, (kinectWidth * 2));
			//var ByteRow = new ArraySegment<byte>(ByteArray, i, (kinectWidth * 2));

			// Reverse the new array
			//Array.Reverse(ByteRow.Array);


			//if (i == 0) {Debug.Log ("reversed ByteRow [321] : " + ByteRow.Array [321]);}

			// Copy reversed image row into the empty reversed array
			//Array.Copy(ByteRow.Array, 0, RevdByteArray, i, (kinectWidth * 2));

		//}// ENDFOR
			
		return ByteArray;

	}//ENDFUNCTION

		



	ushort FilterVal (short [,] FSarray, uint FSlength){
		// FSarray = filter square array with:
		// . Indeces of the pixel (image space) [n, 0] and [n, 1].
		// . Index in the byte array [n, 2].
		// . Value in the byte array/of that pixel [n, 3].
		// FSlength = length of the filter square, initialised before to a max of 24 but can be shorter
		// if on an edge etc..

		// Initialise a table that will hold the values and frequencies of 
		// the pixel values from the filter grid.
		ushort [,] filterCollection = new ushort[FSlength, 2];
		//uint N2047s = 0;

		// go through every row in the filterSquare table 
		for (uint i = 0; i < FSlength; i++){
			// count how many null points there are
		//	if (FSarray [i, 3] == 2047) { 
		//		N2047s += 1;
				// Scan through all entries and add 1 to the freq if a previously detected depth is detected
				for (uint k = 0; k <= i; k ++) {
					if (filterCollection [k, 0] == (ushort) FSarray [i, 3]) { 
						filterCollection [i, 1] += 1;
					}// ENDIF
				} // ENDFOR
				if (filterCollection [i, 0] == 0) {				
					filterCollection [i, 0] = (ushort) FSarray [i, 3];
					filterCollection [i, 1] += 1;
				} // ENDIF
		//	}// ENDIF
		}//ENDFOR

		// Should now have a filter collection table (array) of values and 
		//frequencies from the FilterSquare table and a count of the number of bad pixels (N2047s)

		//want something to say if all values in filter collection are 2047 then null the point



		// Assign returned value
		int? svalue = 500;
		//int?
		//ushort maxFreq = 0;
		int? jval = 0;
		for (int? j = 0; j < FSlength; j++){
			//if ( filterCollection[j, 1] > maxFreq){
			//maxFreq = filterCollection [(int) j, 1];
			svalue += filterCollection [(int) j, 0] * filterCollection [(int) j, 1] ;
			jval = j;
		//	}//ENDIF
		
			if (jval != 0) {
				svalue =  svalue / jval;
			}

		}//ENDFOR

		return (ushort) svalue;

	}//ENDFUNCTION




	//Used to tidy up when application is exited
	void OnApplicationQuit ()
	{
		Debug.Log ("Application ending after " + Time.time + " seconds, closing kinect gracefully.");
		kinect.LED.Color = LEDColor.BlinkGreen;
		kinect.Close ();
	}
		
}
