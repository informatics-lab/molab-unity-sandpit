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

	private static ushort MAX_KINECT_VALUE = 1050;
	private static ushort MIN_KINECT_VALUE = 100;
	private static int ypxl = 480;
	private static int xpxl = 640;

	public Texture2D depthTexture;

	public GameObject terrain; //this is actually a 'Plane' object
	public Material terrainMaterial;
	public Shader terrainMaterialShader;
	public RenderTexture renderTexture;

//	ushort mn = ushort.MaxValue;
//	ushort mx = ushort.MinValue;

	ushort[,] filterCollection;
	short [,] filterSquare;
	int innerBandCount;
	int outerBandCount;
	uint index_filterSquare;

	void Awake ()
	{
		Debug.Log ("Slowing overall framerate");
		Application.targetFrameRate = 1;
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
			terrainWidth = (kinectWidth /10) - 1;
			terrainHeight = (kinectHeight /10) - 1;
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
//		byte[] kinectRawData = e.Data.Data; // This is a 1D 16bit byte array of the raw data 
//		ushort[] kinectDepthValues = new ushort[640 * 480]; // initialise empty unsigned short array to fill
		// for every pixel
//		for (int i = 0; i < kinectDepthValues.Length; i ++) {
//			// convert the uint16 to a ushort
//			ushort s = BitConverter.ToUInt16 (kinectRawData, i*2);
//			kinectDepthValues [i] = s;
//		}

	

		//-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
		Color32[] colors = new Color32[kinectWidth * kinectHeight];
		Color32[] colorsSM = colors;
	
		// size of grid to look at filtering
		int filterGrid = (int) Math.Pow(5,2);
		// look at the surrounding pixels
		//int minus = (int) ( ( ( Math.Sqrt( filterGrid ) / 2 ) - 1 ) );
		//int plus  = (int) ( ( ( Math.Sqrt( filterGrid ) / 2 ) + 1 ) );

		//looping over each integer(pixel value in the array)
		// i is the index of that value in the byte array
		for (int i = 0; i < colors.Length; i ++) {

			// s is the untouched version
			// smooth is the value of the number in the byte array
			ushort s = BitConverter.ToUInt16 (e.Data.Data, i*2);
			ushort smooth = s; //BitConverter.ToUInt16 (e.Data.Data, i*2);

			// ImageColumnIndex and ImageRowIndex are the indeces of that value in the 2d image
			var ImageColumnIndex = i % ypxl; // ypxl == 480, xpxl == 640
			var ImageRowIndex = (i - ImageColumnIndex) / xpxl;

			// if the value of that integer is 2047 i.e. the returned error value
			if (s == 2047) {

				filterSquare = new short[filterGrid-1, 4];
				// scan every pixel in the filter grid

				index_filterSquare = 0;
				// for all pixels in the filter square
				for (int yi = -2; yi < 3; yi++) {
					for (int xi = -2; xi < 3; xi++) {
						// (ignoring the one we are looking at)
						if (xi != 0 || yi != 0) {

							// This is the adjacent pixel indeces
							var ImageColumnIndexADJ = ImageColumnIndex + xi;
							var ImageRowIndexADJ = ImageRowIndex + yi;

							// only consider where the newly calculated indeces exist 
							// (i.e. not greater than the bounds of the image)
							if (ImageColumnIndexADJ >= 0 && ImageColumnIndexADJ <= (xpxl - 1) &&
							    ImageRowIndexADJ    >= 0 && ImageRowIndexADJ    <= (ypxl - 1)) {

								//what is the index in the byte array that corresponds to 
								// the newly calculated adjacent pixel indeces?
								var BAequivI = (ImageRowIndexADJ * ypxl) + ImageColumnIndexADJ;
								var val = BitConverter.ToUInt16 (e.Data.Data, BAequivI*2);

								// Populate filterSquare table (array) with image indeces, byte array index and value.
								filterSquare [index_filterSquare,0] = (short) ImageColumnIndexADJ;//ImageColumnIndex
								filterSquare [index_filterSquare,1] = (short) ImageRowIndexADJ;//ImageRowIndex
								filterSquare [index_filterSquare,2] = (short) BAequivI;//ByteArrayIndex
								filterSquare [index_filterSquare,3] = (short) val;// value at that Byte Array equivalent index
											
								index_filterSquare += 1;

							}//ENDIF
						}//ENDIF
					}//ENDFOR
				}//ENDFOR

				smooth = FilterVal (filterSquare, index_filterSquare);

			}//ENDIF

				
			//smooth = s; //FilterVal(filterSquare);

			if(i == 150){
				smooth = s;//FilterVal (filterSquare, index_filterSquare);
				Debug.Log("smooth 150 : " + s); // 14
				//Debug.Log("filterSquare.length: " + filterSquare.Length); // 96
			}



//
//				// initialise for counting later - how many 0s are in the inner or outerband?
//				innerBandCount = 0;
//				outerBandCount = 0;
//
//								// if the value is not noise
//								if (smooth != 2047)
//								{
//									// We want to count the frequency of each depth
//									// -1 exluding the pixel we are focused on 
//									for (int j = 0; j < (filterGrid -1); j++)
//									{
//										// get the value in the byte array at IADJ
//										ushort ByteADJ = BitConverter.ToUInt16 (e.Data.Data, IADJ*2);
//
//										//add to the count 
//										if (filterCollection[j, 0] == ByteADJ)
//										{
//											// +1 to the count of the frequency column
//											// if that value in the depth array is found again
//											filterCollection[j, 1]++;
//											break;
//										}//ENDIF
//										else if (filterCollection[j, 0] == 0)
//										{
//											//This initialises what goes in the number 
//											//column of the table
//											// also give it 1 in the frequency column
//											filterCollection[j, 0] = ByteADJ;
//											filterCollection[j, 1]++;
//											break;
//										} //ENDELSEIF 
//									}//ENDFOR
//
//									// We will then determine which band the non-2047 pixel
//									// was found in, and increment the band counters.
//									if (yi != (minus / 2) && yi != minus && xi != (minus / 2) && xi != minus) {
//										innerBandCount++;
//									} // ENDIF
//									else {
//										outerBandCount++;
//									}//ENDELSE
//								}//ENDIF
//							}//ENDIF
//						}//ENDIF
//					}//ENDFOR
//				}//ENDFOR
//
//				if (i == 25) {
//
//					Debug.Log ("inner band count : " + innerBandCount);
//					Debug.Log ("outer band count : " + outerBandCount);
//
//				}
//
//
//				// Once we have determined our inner and outer band non-zero counts, and 
//				// accumulated all of those values, we can compare it against the threshold
//				// to determine if our candidate pixel will be changed to the
//				// statistical mode of the non-zero surrounding pixels.
//				int innerBandThreshold = 1;
//				int outerBandThreshold = 1; 
//
//				if (innerBandCount >= innerBandThreshold || outerBandCount >= outerBandThreshold) {
//					ushort frequency = 0;
//					ushort depth = 0;
//					// This loop will determine the statistical mode
//					// of the surrounding pixels for assignment to
//					// the candidate.
//					for (int j = 0; j < (filterGrid -1); j++) {
//						// This means we have reached the end of our
//						// frequency distribution and can break out of the
//						// loop to save time.
//						if (filterCollection [j, 0] == 0)
//							break;
//						if (filterCollection [j, 1] > frequency) {
//							depth = filterCollection [j, 0];
//							frequency = filterCollection [j, 1];
//
//							//put new value into coloursSM array to be passed to the shader
//							Color32 colorSM = new Color32 ();
//							double ScalingFactorSM = 255.0 / (MAX_KINECT_VALUE - MIN_KINECT_VALUE);
//							uint scaledSM = (uint) Math.Ceiling( (depth - MIN_KINECT_VALUE) * ScalingFactorSM); 
//							colorSM.a = 255;
//							colorSM.b = 0;
//							colorSM.g = 0;
//							colorSM.r = (byte)(scaledSM);//SM & 0x000000ff);
//							colorsSM [i] = colorSM;
//
//						}//ENDIF
//					}//ENDFOR
//				}//ENDIF
//			}//ENDIF

			// This version without the noise filter
			Color32 color = new Color32 ();
			double ScalingFactor = 255.0 / (MAX_KINECT_VALUE - MIN_KINECT_VALUE);
			uint scaled = (uint) Math.Ceiling( (s - MIN_KINECT_VALUE) * ScalingFactor); 
			uint scaledSM = (uint) Math.Ceiling( (smooth - MIN_KINECT_VALUE) * ScalingFactor); 
			color.a = 255;
			color.b = 0;
			color.g = 0;
			color.r = (byte)(scaled);// & 0x000000ff);
			colors [i] = color;

			// rehash the color object with the smoothed value in the red channel.
			color.r = (byte) scaledSM;
			colorsSM [i] = color;
				
		}//ENDFOR
			
		//Debug.Log ("filter collection : " + filterCollection);


		//Debug.Log ("min = " + mn);
		//Debug.Log ("max = " + mx);

		// set either colors or colorS
		depthTexture.SetPixels32(colorsSM);
		//depthTexture.filterMode = FilterMode.Trilinear;
		depthTexture.Apply();
		terrainMaterial.SetTexture ("_DepthTex", depthTexture);
		//-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+



	} // ENDFUNCTION




	ushort FilterVal (short [,] FSarray, uint FSlength){

		// Initialise a table that will hold the values and frequencies of 
		// the pixel values in the bands of the filter grid.
		ushort [,] filterCollection = new ushort[FSlength, 2];
		uint N2047s = 0;

		// go through every row in the filterSquare table 
		for (uint i = 0; i < FSlength; i++){
			// count how many null points there are
			if ( FSarray[i, 3] == 2047) { 
				N2047s += 1;
				// add 1 to the count if a previously detected depth is detected
			} else if (	filterCollection[i,0] == (ushort) FSarray[i, 3]){
				filterCollection[i, 1] += 1;
				// add the entry and 1 to the count if a new depth is detected
			} else if ( filterCollection[i, 0] == 0){				
				filterCollection[i, 0] = (ushort) FSarray[i, 3];
				filterCollection[i, 1] += 1;
			} // ENDIF
		}//ENDFOR
			
		// Should now have a filter collection table (array) of values and 
		//frequencies from the FilterSquare table and a count of the number of dead pixels (N2047s)

		// Assign returned value
		int? svalue = 0;
		if ( N2047s <= 5){
			uint maxFreq = 0;
			for (uint j = 0; j < FSlength; j++){
				if ( filterCollection[j, 1] > maxFreq){
					svalue = filterCollection[j, 0];
				} //ENDIF
			}//ENDFOR
		}//ENDIF

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
