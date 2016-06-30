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

	ushort mn = ushort.MaxValue;
	ushort mx = ushort.MinValue;


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
	

		//looping over each integer(pixel value in the array)
		for (int i = 0; i < colors.Length; i ++) {

			// s is the untouched version
			// smooth is the value of the number in the byte array
			// i is the index of that value in the byte array
			// ImageColumnIndex and ImageRowIndex are the indeces of that value in the 2d image
			ushort s = BitConverter.ToUInt16 (e.Data.Data, i*2);
			ushort smooth = BitConverter.ToUInt16 (e.Data.Data, i*2);



//			if (s > mx && s != 2047) {
//				mx = s; 
//			}
//			if (s < mn && s != 0) {
//				mn = s;
//			}

			// saturate values above or below the limit
			if (smooth > MAX_KINECT_VALUE) {
				smooth = MAX_KINECT_VALUE;
			}
			if (smooth < MIN_KINECT_VALUE) {
				smooth = MIN_KINECT_VALUE;
			}


			if (s > MAX_KINECT_VALUE || s < MIN_KINECT_VALUE ) {
				s = MIN_KINECT_VALUE;
			}


			var ImageColumnIndex = i % ypxl; // ypxl == 480, xpxl == 640
			var ImageRowIndex = (i - ImageColumnIndex) / xpxl;

			// if the value of that integer is 0
			if (smooth == 0) {
				
				// Table that will hold the values and frequencies 
				// of the pixel values in the bands
				ushort[,] filterCollection = new ushort[24,2];
				// initialise for counting later - how many 0s are in the inner or outerband?
				int innerBandCount = 0;
				int outerBandCount = 0;
			
				// look at the surrounding pixels
				for (int yi = -2; yi < 3; yi++) {
					for (int xi = -2; xi < 3; xi++) {
						// (ignoring the one we are looking at)
						if (xi != 0 || yi != 0) {

							// This is the adjacent pixel indeces
							var ImageColumnIndexADJ = ImageColumnIndex + xi;
							var ImageRowIndexADJ = ImageRowIndex + yi;

							// only consider where the newly calculated indeces exist 
							// (i.e. not greater than the bounds of the image)
							if (ImageColumnIndexADJ >= 0 && ImageColumnIndexADJ <= (xpxl-1) && 
								ImageRowIndexADJ >= 0 && ImageRowIndexADJ <= (ypxl-1))
							{

								//what is the index in the byte array that corresponds to 
								// the newly calculated adjacent pixel indeces?
								var IADJ = (ImageRowIndexADJ * ypxl) + ImageColumnIndexADJ;

								// if we have found some noise
								if (smooth != 0)
								{
									// We want to find count the frequency of each depth
									// 24 because we have a 5 by 5 grid exluding the 
									// pixel we are focused on 
									for (int j = 0; j < 24; j++)
									{
										// get the value in the byte array at IADJ
										ushort ByteADJ = BitConverter.ToUInt16 (e.Data.Data, IADJ*2);

										//add the count 
										if (filterCollection[j, 0] == ByteADJ)
										{
											// +1 to the count of the frequency column
											// if that value in the depth array is found again
											filterCollection[j, 1]++;
											break;
										}//ENDIF
										else if (filterCollection[j, 0] == 0)
										{
											//This initialises what goes in the number 
											//column of the table
											// also give it 1 in the frequency column
											filterCollection[j, 0] = ByteADJ;
											filterCollection[j, 1]++;
											break;
										} //ENDELSEIF 
									}//ENDFOR

									// We will then determine which band the non-0 pixel
									// was found in, and increment the band counters.
									if (yi != 2 && yi != -2 && xi != 2 && xi != -2)
										innerBandCount++;
									else
										outerBandCount++;
								}//ENDIF
							}//ENDIF
						}//ENDIF
					}//ENDFOR
				}//ENDFOR

				// Once we have determined our inner and outer band non-zero counts, and 
				// accumulated all of those values, we can compare it against the threshold
				// to determine if our candidate pixel will be changed to the
				// statistical mode of the non-zero surrounding pixels.
				int innerBandThreshold = 1;
				int outerBandThreshold = 1; 

				if (innerBandCount >= innerBandThreshold || outerBandCount >= outerBandThreshold) {
					ushort frequency = 0;
					ushort depth = 0;
					// This loop will determine the statistical mode
					// of the surrounding pixels for assignment to
					// the candidate.
					for (int j = 0; j < 24; j++) {
						// This means we have reached the end of our
						// frequency distribution and can break out of the
						// loop to save time.
						if (filterCollection [j, 0] == 0)
							break;
						if (filterCollection [j, 1] > frequency) {
							depth = filterCollection [j, 0];
							frequency = filterCollection [j, 1];

							//put new value into coloursSM array to be passed to the shader
							Color32 colorSM = new Color32 ();
							double ScalingFactorSM = 255.0 / (MAX_KINECT_VALUE - MIN_KINECT_VALUE);
							uint scaledSM = (uint) Math.Ceiling( (depth - MIN_KINECT_VALUE) * ScalingFactorSM); 
							colorSM.a = 255;
							colorSM.b = 0;
							colorSM.g = 0;
							colorSM.r = (byte)(scaledSM);//SM & 0x000000ff);
							colorsSM [i] = colorSM;

						}//ENDIF
					}//ENDFOR
				}//ENDIF
			}//ENDIF
	

			// This version without the noise filter
			Color32 color = new Color32 ();
			double ScalingFactor = 255.0 / (MAX_KINECT_VALUE - MIN_KINECT_VALUE);
			uint scaled = (uint) Math.Ceiling( (s - MIN_KINECT_VALUE) * ScalingFactor); 
			color.a = 255;
			color.b = 0;
			color.g = 0;
			color.r = (byte)(scaled);// & 0x000000ff);
			colors [i] = color;

		}//ENDFOR;


		Debug.Log ("min = " + mn);
		Debug.Log ("max = " + mx);

		// set either colors or colorS
		depthTexture.SetPixels32(colorsSM);
		//depthTexture.filterMode = FilterMode.Trilinear;
		depthTexture.Apply();
		terrainMaterial.SetTexture ("_DepthTex", depthTexture);
		//-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+



	}

	//Used to tidy up when application is exited
	void OnApplicationQuit ()
	{
		Debug.Log ("Application ending after " + Time.time + " seconds, closing kinect gracefully.");
		kinect.LED.Color = LEDColor.BlinkGreen;
		kinect.Close ();
	}
		
}
