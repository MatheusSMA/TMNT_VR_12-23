#if UNITY_5_6_0 || UNITY_5_6_1 
	#define AVPRO_MOVIECAPTURE_UNITYBUG_RENDERTOCUBEMAP_56
#endif
#if UNITY_2018_1_OR_NEWER
	// Unity 2018.1 introduces stereo cubemap render methods, but with no camera rotation
	#define AVPRO_MOVIECAPTURE_UNITY_STEREOCUBEMAP_RENDER
	#if UNITY_2018_2_OR_NEWER || UNITY_2018_1_9
		// Unity 2018.2 adds camera rotation
		#define AVPRO_MOVIECAPTURE_UNITY_STEREOCUBEMAP_RENDER_WITHROTATION
	#endif
#endif
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

//-----------------------------------------------------------------------------
// Copyright 2012-2022 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProMovieCapture
{
	/// <summary>
	/// Capture a camera view in 360 equi-rectangular format.  
	/// The camera is rendered into a cubemap, so the scene is rendered an extra 6 times.
	/// Finally the cubemap is converted to equi-rectangular format and encoded.
	/// </summary>
	//[RequireComponent(typeof(Camera))]
	[AddComponentMenu("AVPro Movie Capture/Capture From Camera 360 (VR)", 100)]
	public class CaptureFromCamera360 : CaptureBase
	{
		[SerializeField] CameraSelector _cameraSelector = null;

		public CameraSelector CameraSelector
		{
			get { return _cameraSelector; }
			set { _cameraSelector = value; }
		}		

		[SerializeField] Camera _camera = null;

		[SerializeField] CubemapResolution _cubemapResolution = CubemapResolution.POW2_2048;
		[SerializeField] CubemapDepth _cubemapDepth = CubemapDepth.Depth_24;

		public CubemapResolution CubemapFaceResolution
		{
			get { return _cubemapResolution; }
			set { _cubemapResolution = value; }
		}
		public CubemapDepth CubemapDepthResolution
		{
			get { return _cubemapDepth; }
			set { _cubemapDepth = value; }
		}

		[SerializeField] bool _supportGUI = false;
		[SerializeField] bool _supportCameraRotation = false;
		[SerializeField] bool _onlyLeftRightRotation = false;

		public bool SupportGUI
		{
			get { return _supportGUI; }
			set { _supportGUI = value; }
		}
		public bool SupportCameraRotation
		{
			get { return _supportCameraRotation; }
			set { _supportCameraRotation = value; }
		}
		public bool OnlyLeftRightRotation
		{
			get { return _onlyLeftRightRotation; }
			set { _onlyLeftRightRotation = value; }
		}
	
		[Tooltip("Render 180 degree equirectangular instead of 360 degrees")]
		[SerializeField] bool _render180Degrees = false;
		[SerializeField] StereoPacking _stereoRendering = StereoPacking.None;
		
		public bool Render180Degrees
		{
			get { return _render180Degrees; }
			set { _render180Degrees = value; }
		}
		
		public StereoPacking StereoRendering
		{
			get { return _stereoRendering; }
			set { _stereoRendering = value; }
		}

		[Tooltip("Makes assumption that 1 Unity unit is 1m")]
		[SerializeField] float _ipd = 0.064f;

		public float IPD
		{
			get { return _ipd; }
			set { _ipd = value; }
		}

		[Tooltip("Percentage cube faces are overdrawn each edge then blended to alleviate screen space FX seams")]
		[SerializeField] float _blendOverlapPercent = 0.0f;

		// State
		private RenderTexture _faceTarget;
		// RJT TODO: Texture array?
		private RenderTexture[] _faceTargets = new RenderTexture[6];
		private Material _blitMaterial;
		private Material _cubemapToEquirectangularMaterial;
		private RenderTexture _cubeTarget;
		private RenderTexture _finalTarget;
		private System.IntPtr _targetNativePointer = System.IntPtr.Zero;
		private int _propFlipX;
		#if SUPPORT_SHADER_ROTATION
		private int _propRotation;
		#endif

		private enum CubemapRenderMethod
		{
			Manual,		// Manually render the cubemaps - supports world space GUI, camera rotation, but is slow and doesn't give correct stereo - *Required for blend overlap*
			Unity,		// No stereo, no world space GUI, no camera rotation
			Unity2018,	// Good fast stereo, no world space GUI, camera rotation only in 2018.2 and above
		}

		public CaptureFromCamera360()
		{
			// Override the default values to match more common use cases for this capture component
			_renderResolution = Resolution.POW2_2048x2048;
		}

		private CubemapRenderMethod GetCubemapRenderingMethod()
		{
			if (_supportGUI || (_blendOverlapPercent > 0.0f))
			{
				return CubemapRenderMethod.Manual;
			}
			if (_supportCameraRotation)
			{
#if AVPRO_MOVIECAPTURE_UNITY_STEREOCUBEMAP_RENDER_WITHROTATION
				return CubemapRenderMethod.Unity2018;
#else
				return CubemapRenderMethod.Manual;
#endif
			}
			if (_stereoRendering == StereoPacking.None)
			{
#if AVPRO_MOVIECAPTURE_UNITY_STEREOCUBEMAP_RENDER_WITHROTATION
				return CubemapRenderMethod.Unity2018;
#else
				return CubemapRenderMethod.Unity;
#endif
			}
			else
			{
#if AVPRO_MOVIECAPTURE_UNITY_STEREOCUBEMAP_RENDER
				return CubemapRenderMethod.Unity2018;
#else
				return CubemapRenderMethod.Manual;
#endif
			}
		}

		public void SetCamera(Camera camera)
		{
			_camera = camera;
		}

#if false
    private void OnRenderImage(RenderTexture source, RenderTexture dest)
	{
#if false
		if (_capturing && !_paused)
		{
			while (_handle >= 0 && !NativePlugin.IsNewFrameDue(_handle))
			{
				System.Threading.Thread.Sleep(1);
			}
			if (_handle >= 0)
			{
                if (_audioCapture && _audioDeviceIndex < 0 && !_noAudio)
                {
                    uint bufferLength = (uint)_audioCapture.BufferLength;
                    if (bufferLength > 0)
                    {
                        NativePlugin.EncodeAudio(_handle, _audioCapture.BufferPtr, bufferLength);
                        _audioCapture.FlushBuffer();
                    }
                }

                // In Direct3D the RT can be flipped vertically
                /*if (source.texelSize.y < 0)
                {

                }*/

				Graphics.Blit(_cubeTarget, _target, _cubemapToEquirectangularMaterial);

				RenderThreadEvent(NativePlugin.PluginEvent.CaptureFrameBuffer);
				GL.InvalidateState();
				
				UpdateFPS();
			}
		}
#endif
		// Pass-through

		if (_cubeTarget != null)
		{
			Graphics.Blit(_cubeTarget, dest, _cubemapToEquirectangularMaterial);
		}
		else
		{
			Graphics.Blit(source, dest);
		}
	}
#endif

		public override void UpdateFrame()
		{
			if (_cameraSelector != null)
			{
				if (_camera != _cameraSelector.Camera)
				{
					SetCamera(_cameraSelector.Camera);
				}
			}

			if (_useWaitForEndOfFrame)
			{
				if (_capturing && !_paused)
				{
					StartCoroutine(FinalRenderCapture());
				}
			}
			else
			{
				Capture();
			}
			base.UpdateFrame();
		}

		private IEnumerator FinalRenderCapture()
		{
			yield return _waitForEndOfFrame;

			Capture();
		}

		private void Capture()
		{
			TickFrameTimer();

			AccumulateMotionBlur();

			if (_capturing && !_paused)
			{
				if (
					((_cubeTarget != null) || (_faceTargets[0] != null)) &&
					(_camera != null)
				)
				{
					bool canGrab = true;

					if (IsUsingMotionBlur())
					{
						// TODO: fix motion blur
						//this._motionBlur.RenderImage()
						// If the motion blur is still accumulating, don't grab this frame
						canGrab = _motionBlur.IsFrameAccumulated;
					}

					if (canGrab && CanOutputFrame())
					{
						EncodeUnityAudio();

						RenderTexture finalTexture = _finalTarget;
						if (!IsUsingMotionBlur())
						{
							UpdateTexture();
						}
						else
						{
							finalTexture = _motionBlur.FinalTexture;
						}

						// Side-by-side transparency
						RenderTexture newSourceTexture = UpdateForSideBySideTransparency( finalTexture );
						if (newSourceTexture)
						{
							finalTexture = newSourceTexture;
						}

						if (_targetNativePointer == System.IntPtr.Zero || _supportTextureRecreate)
						{
							// NOTE: If support for captures to survive through alt-tab events, or window resizes where the GPU resources are recreated
							// is required, then this line is needed.  It is very expensive though as it does a sync with the rendering thread.
							_targetNativePointer = finalTexture.GetNativeTexturePtr();
						}

						NativePlugin.SetTexturePointer(_handle, _targetNativePointer);

						RenderThreadEvent(NativePlugin.PluginEvent.CaptureFrameBuffer);

						// ADG NOTE: Causes screen flickering under D3D12, even if we're not doing any rendering at native level
						// And also seems to cause GL.sRGBWrite to be set to false, which causes screen darkening in Linear mode
						if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D12)
						{
							GL.InvalidateState();
						}

						UpdateFPS();
					}
				}
			}

			RenormTimer();
		}

		private static void ClearCubemap(RenderTexture texture, Color color)
		{
			// TODO: Find a better way to do this?
			bool clearDepth = (texture.depth != 0);
			Graphics.SetRenderTarget(texture, 0, CubemapFace.PositiveX);
			GL.Clear(true, clearDepth, color);
			Graphics.SetRenderTarget(texture, 0, CubemapFace.PositiveY);
			GL.Clear(true, clearDepth, color);
			Graphics.SetRenderTarget(texture, 0, CubemapFace.PositiveZ);
			GL.Clear(true, clearDepth, color);
			Graphics.SetRenderTarget(texture, 0, CubemapFace.NegativeX);
			GL.Clear(true, clearDepth, color);
			Graphics.SetRenderTarget(texture, 0, CubemapFace.NegativeY);
			GL.Clear(true, clearDepth, color);
			Graphics.SetRenderTarget(texture, 0, CubemapFace.NegativeZ);
			GL.Clear(true, clearDepth, color);
			Graphics.SetRenderTarget(null);
		}

		private void RenderCubemapToEquiRect(RenderTexture cubemap, RenderTexture target, bool supportRotation, Quaternion rotation, bool isEyeLeft)
		{
			#if SUPPORT_SHADER_ROTATION
			if (supportRotation)
			{
				// Note: Because Unity's Camera.RenderCubemap() doesn't support rotated cameras, we apply the rotation matrix in the cubemap lookup
				_cubemapToEquirectangularMaterial.EnableKeyword("USE_ROTATION");
				Matrix4x4 rotationMatrix = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
				_cubemapToEquirectangularMaterial.SetMatrix(_propRotation, rotationMatrix);
			}
			else
			{
				_cubemapToEquirectangularMaterial.DisableKeyword("USE_ROTATION");
			}
			#endif

			if (_stereoRendering == StereoPacking.TopBottom)
			{
				if (isEyeLeft)
				{
					// Render to top
					_cubemapToEquirectangularMaterial.DisableKeyword("STEREOPACK_BOTTOM");
					_cubemapToEquirectangularMaterial.EnableKeyword("STEREOPACK_TOP");
				}
				else
				{
					// Render to bottom
					_cubemapToEquirectangularMaterial.DisableKeyword("STEREOPACK_TOP");
					_cubemapToEquirectangularMaterial.EnableKeyword("STEREOPACK_BOTTOM");
				}
			}
			else if (_stereoRendering == StereoPacking.LeftRight)
			{
				if (isEyeLeft)
				{
					// Render to left
					_cubemapToEquirectangularMaterial.DisableKeyword("STEREOPACK_RIGHT");
					_cubemapToEquirectangularMaterial.EnableKeyword("STEREOPACK_LEFT");
				}
				else
				{
					// Render to right
					_cubemapToEquirectangularMaterial.DisableKeyword("STEREOPACK_LEFT");
					_cubemapToEquirectangularMaterial.EnableKeyword("STEREOPACK_RIGHT");
				}
			}

			// Between Unity 2018.1.0 and 2018.3.0 Unity doesn't seem to set the correct sRGBWrite state and keeps it as false
			#if (UNITY_2018_1_OR_NEWER && !UNITY_2018_3_OR_NEWER)
			bool sRGBWritePrev = GL.sRGBWrite;
			GL.sRGBWrite = target.sRGB;
			#endif

			// Blending?
			if (_blendOverlapPercent > 0.0f)
			{
				_cubemapToEquirectangularMaterial.EnableKeyword("USE_BLENDING");
				_cubemapToEquirectangularMaterial.SetTexture("_Face_Left",	_faceTargets[0]);
				_cubemapToEquirectangularMaterial.SetTexture("_Face_Right", _faceTargets[1]);
				_cubemapToEquirectangularMaterial.SetTexture("_Face_Up",	_faceTargets[2]);
				_cubemapToEquirectangularMaterial.SetTexture("_Face_Down",	_faceTargets[3]);
				_cubemapToEquirectangularMaterial.SetTexture("_Face_Front", _faceTargets[4]);
				_cubemapToEquirectangularMaterial.SetTexture("_Face_Back",	_faceTargets[5]);
				_cubemapToEquirectangularMaterial.SetFloat("_inverseOverlap", (1.0f / (1.0f + (_blendOverlapPercent * 0.01f))));
			}
			else { _cubemapToEquirectangularMaterial.DisableKeyword("USE_BLENDING"); }

			// Render
			Graphics.Blit(cubemap, target, _cubemapToEquirectangularMaterial);

			#if (UNITY_2018_1_OR_NEWER && !UNITY_2018_3_OR_NEWER)
			GL.sRGBWrite = sRGBWritePrev;
			#endif
		}

		private void UpdateTexture()
		{
			// Between Unity 2018.1.0 and 2018.3.0 Unity doesn't seem to set the correct sRGBWrite state and keeps it as false
			#if (UNITY_2018_1_OR_NEWER && !UNITY_2018_3_OR_NEWER)
			bool sRGBWritePrev = GL.sRGBWrite;
			GL.sRGBWrite = _cubeTarget.sRGB;
			#endif

			// In Direct3D the RT can be flipped vertically
			/*if (source.texelSize.y < 0)
			{

			}*/

			//_cubeCamera.transform.position = _camera.transform.position;
			//_cubeCamera.transform.rotation = _camera.transform.rotation;

			Camera camera = _camera;

			Rect prevRect = camera.rect;
			camera.rect = new Rect(0f, 0f, 1f, 1f);		// NOTE: the Scene View camera will change it's rect when being interacted with, so we need to make sure it's overridden					

			CubemapRenderMethod cubemapRenderMethod = GetCubemapRenderingMethod();
			
			if (_stereoRendering == StereoPacking.None)
			{
				if (cubemapRenderMethod == CubemapRenderMethod.Unity)
				{
					#if AVPRO_MOVIECAPTURE_UNITYBUG_RENDERTOCUBEMAP_56
					RenderTexture prev = camera.targetTexture;
					#endif
					
					// Note: Camera.RenderToCubemap() doesn't support camera rotation
					camera.RenderToCubemap(_cubeTarget, 63);

					#if AVPRO_MOVIECAPTURE_UNITYBUG_RENDERTOCUBEMAP_56
					// NOTE: We need this to clean up the state in at least Unity 5.6.0 - 5.6.1p1
					camera.targetTexture = prev;
					#endif
				}
				#if AVPRO_MOVIECAPTURE_UNITY_STEREOCUBEMAP_RENDER
				else if (cubemapRenderMethod == CubemapRenderMethod.Unity2018)
				{
					// Note: If we use Mono instead of Left then rotation isn't supported
					// - RJT NOTE: Looks like a typo, have changed from 'this.' to 'camera.' which is used _everywhere_ else
					if (/*this*/camera.transform.rotation == Quaternion.identity)
					{
						camera.RenderToCubemap(_cubeTarget, 63, Camera.MonoOrStereoscopicEye.Mono);
					}
					else
					{
						camera.stereoSeparation = 0f;
						camera.RenderToCubemap(_cubeTarget, 63, Camera.MonoOrStereoscopicEye.Left);
					}
				}
				#endif
				else if (cubemapRenderMethod == CubemapRenderMethod.Manual)
				{
					RenderCameraToCubemap(camera, _cubeTarget);
				}
				RenderCubemapToEquiRect(_cubeTarget, _finalTarget, false, Quaternion.identity, true);
			}
			else
			{
				#if AVPRO_MOVIECAPTURE_UNITY_STEREOCUBEMAP_RENDER
				if (cubemapRenderMethod == CubemapRenderMethod.Unity2018)
				{
					//Left eye
					camera.stereoSeparation = _ipd;
					camera.RenderToCubemap(_cubeTarget, 63, Camera.MonoOrStereoscopicEye.Left);
					RenderCubemapToEquiRect(_cubeTarget, _finalTarget, false, camera.transform.rotation, true);

					// Right eye
					_cubeTarget.DiscardContents();
					camera.RenderToCubemap(_cubeTarget, 63, Camera.MonoOrStereoscopicEye.Right);
					RenderCubemapToEquiRect(_cubeTarget, _finalTarget, false, camera.transform.rotation, false);
				} else
				#endif
				if (cubemapRenderMethod == CubemapRenderMethod.Manual)
				{
					// Save camera state
					Vector3 cameraPosition = camera.transform.localPosition;

					// Left eye
					camera.transform.Translate(new Vector3(-_ipd / 2f, 0f, 0f), Space.Self);
					RenderCameraToCubemap(camera, _cubeTarget);
					RenderCubemapToEquiRect(_cubeTarget, _finalTarget, false, Quaternion.identity, true);

					// Right eye
					camera.transform.localPosition = cameraPosition;
					camera.transform.Translate(new Vector3(_ipd / 2f, 0f, 0f), Space.Self);
					RenderCameraToCubemap(camera, _cubeTarget);
					RenderCubemapToEquiRect(_cubeTarget, _finalTarget, false, Quaternion.identity, false);

					// Restore camera state
					camera.transform.localPosition = cameraPosition;
				}
			}

			camera.rect = prevRect;

			#if (UNITY_2018_1_OR_NEWER && !UNITY_2018_3_OR_NEWER)
			GL.sRGBWrite = sRGBWritePrev;
			#endif
		}

		private void RenderCameraToCubemap(Camera camera, RenderTexture cubemapTarget)
		{
			RenderTexture prevRT = RenderTexture.active;

			// Cache old camera values
			float prevFieldOfView = camera.fieldOfView;
			RenderTexture prevtarget = camera.targetTexture;
			Quaternion prevRotation = camera.transform.rotation;

			// Ignore the camera rotation
			Quaternion xform = camera.transform.rotation;
			if (!_supportCameraRotation)
			{
				xform = Quaternion.identity;
			}
			else if (_onlyLeftRightRotation)
			{
				xform = Quaternion.Euler(0f, camera.transform.eulerAngles.y, 0f);
			}

			if (_blendOverlapPercent > 0.0f)
			{
				// Adjust FOV for overlap percentage (each each)
				camera.fieldOfView = (Mathf.Atan(1.0f + (_blendOverlapPercent * 0.01f)) * 2.0f * Mathf.Rad2Deg);

				// RJT TODO: LUT/loop

				// Left
				camera.transform.rotation = xform * Quaternion.LookRotation(Vector3.right, Vector3.down);
				camera.targetTexture = _faceTargets[0];
				camera.Render();

				// Right
				camera.transform.rotation = xform * Quaternion.LookRotation(Vector3.left, Vector3.down);
				camera.targetTexture = _faceTargets[1];
				camera.Render();

				// Up
				camera.transform.rotation = xform * Quaternion.LookRotation(Vector3.up, Vector3.forward);
				camera.targetTexture = _faceTargets[2];
				camera.Render();

				// Down
				camera.transform.rotation = xform * Quaternion.LookRotation(Vector3.down, Vector3.back);
				camera.targetTexture = _faceTargets[3];
				camera.Render();

				// Front
				camera.transform.rotation = xform * Quaternion.LookRotation(Vector3.forward, Vector3.down);
				camera.targetTexture = _faceTargets[4];
				camera.Render();

				// Back
				camera.transform.rotation = xform * Quaternion.LookRotation(Vector3.back, Vector3.down);
				camera.targetTexture = _faceTargets[5];
				camera.Render();
			}
			else
			{
				// NOTE: There is a bug in Unity 2017.1.0f3 to at least 2017.2beta7 which causes deferred rendering mode to always clear the cubemap target to white

				camera.fieldOfView = 90f;
				camera.targetTexture = _faceTarget;
	
				// Front
				camera.transform.rotation = xform * Quaternion.LookRotation(Vector3.forward, Vector3.down);
				_faceTarget.DiscardContents();
				camera.Render();
				Graphics.SetRenderTarget(cubemapTarget, 0, CubemapFace.PositiveZ);
				Graphics.Blit(_faceTarget, _blitMaterial);

				// Back
				camera.transform.rotation = xform * Quaternion.LookRotation(Vector3.back, Vector3.down);
				_faceTarget.DiscardContents();
				camera.Render();
				Graphics.SetRenderTarget(cubemapTarget, 0, CubemapFace.NegativeZ);
				Graphics.Blit(_faceTarget, _blitMaterial);

				// Right
				camera.transform.rotation = xform * Quaternion.LookRotation(Vector3.right, Vector3.down);
				_faceTarget.DiscardContents();
				camera.Render();
				Graphics.SetRenderTarget(cubemapTarget, 0, CubemapFace.NegativeX);
				Graphics.Blit(_faceTarget, _blitMaterial);

				// Left
				camera.transform.rotation = xform * Quaternion.LookRotation(Vector3.left, Vector3.down);
				_faceTarget.DiscardContents();
				camera.Render();
				Graphics.SetRenderTarget(cubemapTarget, 0, CubemapFace.PositiveX);
				Graphics.Blit(_faceTarget, _blitMaterial);

				// Up
				camera.transform.rotation = xform * Quaternion.LookRotation(Vector3.up, Vector3.forward);
				_faceTarget.DiscardContents();
				camera.Render();
				Graphics.SetRenderTarget(cubemapTarget, 0, CubemapFace.PositiveY);
				Graphics.Blit(_faceTarget, _blitMaterial);

				// Down
				camera.transform.rotation = xform * Quaternion.LookRotation(Vector3.down, Vector3.back);
				_faceTarget.DiscardContents();
				camera.Render();
				Graphics.SetRenderTarget(cubemapTarget, 0, CubemapFace.NegativeY);
				Graphics.Blit(_faceTarget, _blitMaterial);

				Graphics.SetRenderTarget(prevRT);
			}

			// Restore camera values
			camera.transform.rotation = prevRotation;
			camera.fieldOfView = prevFieldOfView;
			camera.targetTexture = prevtarget;
		}

		private void AccumulateMotionBlur()
		{
			if (_motionBlur != null)
			{
				if (_capturing && !_paused)
				{
					if (_camera != null && _handle >= 0)
					{
						UpdateTexture();
						_motionBlur.Accumulate(_finalTarget);
					}
				}
			}
		}

		public override bool PrepareCapture()
		{
			if (_capturing)
			{
				return false;
			}
#if UNITY_EDITOR_WIN || (!UNITY_EDITOR && UNITY_STANDALONE_WIN)
			if (SystemInfo.graphicsDeviceVersion.StartsWith("Direct3D 9"))
			{
				Debug.LogError("[AVProMovieCapture] Direct3D9 not yet supported, please use Direct3D11 instead.");
				return false;
			}
			else if (SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL") && !SystemInfo.graphicsDeviceVersion.Contains("emulated"))
			{
				Debug.LogError("[AVProMovieCapture] OpenGL not yet supported for CaptureFromCamera360 component, please use Direct3D11 instead. You may need to switch your build platform to Windows.");
				return false;
			}
#endif
			// Check cubemap resolution support
			int cubemapResolution = (int)_cubemapResolution;
			if (_blendOverlapPercent > 0.0f)
			{
				// Adjust resolution for overlap percentage (each edge)
				cubemapResolution = (int)(((float)cubemapResolution * (1.0f + ((_blendOverlapPercent * 0.01f) * 2.0f))) + 0.5f);
			}
			if (cubemapResolution > SystemInfo.maxCubemapSize)
			{
				cubemapResolution = SystemInfo.maxCubemapSize;
				Debug.LogWarning("[AVProMovieCapture] Reducing cubemap size to system max: " + cubemapResolution);
			}

			// Setup material
			_pixelFormat = NativePlugin.PixelFormat.RGBA32;
			_isTopDown = true;
			if (_cameraSelector != null)
			{
				//if (_camera != _cameraSelector.Camera)
				{
					SetCamera(_cameraSelector.Camera);
				}
			}
			if (_camera == null)
			{
				SetCamera(this.GetComponent<Camera>());
			}
			if (_camera == null)
			{
				Debug.LogError("[AVProMovieCapture] No camera assigned to CaptureFromCamera360");
				return false;
			}

			// Resolution
			int finalWidth = Mathf.FloorToInt(_camera.pixelRect.width);
			int finalHeight = Mathf.FloorToInt(_camera.pixelRect.height);
			if (_renderResolution == Resolution.Custom)
			{
				finalWidth = (int)_renderSize.x;
				finalHeight = (int)_renderSize.y;
			}
			else if (_renderResolution != Resolution.Original)
			{
				GetResolution(_renderResolution, ref finalWidth, ref finalHeight);
			}

			// Setup rendering a different render target if we're overriding resolution or anti-aliasing
			//if (_renderResolution != Resolution.Original || _renderAntiAliasing != QualitySettings.antiAliasing)
			{
				int aaLevel = GetCameraAntiAliasingLevel(_camera);

				CubemapRenderMethod cubemapRenderMethod = GetCubemapRenderingMethod();
				Debug.Log("[AVProMovieCapture] Using cubemap render method: " + cubemapRenderMethod.ToString());

				// Create the final render target
				_targetNativePointer = System.IntPtr.Zero;
				if (_finalTarget != null)
				{
					_finalTarget.DiscardContents();
					if (_finalTarget.width != finalWidth || _finalTarget.height != finalHeight)
					{
						RenderTexture.ReleaseTemporary(_finalTarget);
						_finalTarget = null;
					}
				}
				if (_finalTarget == null)
				{
					_finalTarget = RenderTexture.GetTemporary(finalWidth, finalHeight, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 1);
					_finalTarget.name = "[AVProMovieCapture] 360 Final Target";
				}

				// Create the per-face render target (only when need to support GUI rendering)
				if (_faceTarget != null)
				{
					_faceTarget.DiscardContents();
					if (_faceTarget.width != (int)cubemapResolution || 
						_faceTarget.height != (int)cubemapResolution || 
						_faceTarget.depth != (int)_cubemapDepth || 
						_faceTarget.antiAliasing != aaLevel)
					{
						RenderTexture.Destroy(_faceTarget);
						_faceTarget = null;
					}
				}

				if (_faceTargets[0] != null)
				{
					for (int i = 0; i < 6; ++i )
					{
						_faceTargets[i].DiscardContents();
					}

					if (_faceTargets[0].width != cubemapResolution || 
						_faceTargets[0].height != cubemapResolution || 
						_faceTargets[0].depth != (int)_cubemapDepth || 
						_faceTargets[0].antiAliasing != aaLevel)
					{
						for (int i = 0; i < 6; ++i )
						{
							RenderTexture.Destroy(_faceTargets[i]);
							_faceTargets[i] = null;
						}
					}
				}

				RenderTextureFormat renderTextureFormat = RenderTextureFormat.Default;
#if USING_URP
				renderTextureFormat = RenderTextureFormat.ARGBHalf;//ARGBFloat;
#endif
				if (cubemapRenderMethod == CubemapRenderMethod.Manual)
				{
					if ((_blendOverlapPercent > 0.0f) && (_faceTargets[0] == null))
					{
						for (int i = 0; i < 6; ++i )
						{
							_faceTargets[i] = new RenderTexture(cubemapResolution, cubemapResolution, (int)_cubemapDepth, renderTextureFormat, RenderTextureReadWrite.Default);
							_faceTargets[i].name = "[AVProMovieCapture] 360 Face Target";
							_faceTargets[i].isPowerOfTwo = Mathf.IsPowerOfTwo(cubemapResolution);
							_faceTargets[i].wrapMode = TextureWrapMode.Clamp;
							_faceTargets[i].filterMode = FilterMode.Bilinear;
							_faceTargets[i].autoGenerateMips = false;
							_faceTargets[i].antiAliasing = aaLevel;
						}
					}
					else if (_faceTarget == null)
					{
						_faceTarget = new RenderTexture((int)cubemapResolution, (int)cubemapResolution, (int)_cubemapDepth, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
						_faceTarget.name = "[AVProMovieCapture] 360 Face Target";
						_faceTarget.isPowerOfTwo = true;
						_faceTarget.wrapMode = TextureWrapMode.Clamp;
						_faceTarget.filterMode = FilterMode.Bilinear;
						_faceTarget.autoGenerateMips = false;
						_faceTarget.antiAliasing = aaLevel;
					}

					_cubemapToEquirectangularMaterial.SetFloat(_propFlipX, 0.0f);
				}
				else
				{
					// Unity's RenderToCubemap result needs flipping
					_cubemapToEquirectangularMaterial.SetFloat(_propFlipX, 1.0f);
				}

				_cubemapToEquirectangularMaterial.DisableKeyword("USE_ROTATION");
				_cubemapToEquirectangularMaterial.DisableKeyword("STEREOPACK_TOP");
				_cubemapToEquirectangularMaterial.DisableKeyword("STEREOPACK_BOTTOM");
				_cubemapToEquirectangularMaterial.DisableKeyword("STEREOPACK_LEFT");
				_cubemapToEquirectangularMaterial.DisableKeyword("STEREOPACK_RIGHT");

				if (_render180Degrees)
				{
					_cubemapToEquirectangularMaterial.DisableKeyword("LAYOUT_EQUIRECT360");
					_cubemapToEquirectangularMaterial.EnableKeyword("LAYOUT_EQUIRECT180");
				}
				else
				{
					_cubemapToEquirectangularMaterial.DisableKeyword("LAYOUT_EQUIRECT180");
					_cubemapToEquirectangularMaterial.EnableKeyword("LAYOUT_EQUIRECT360");
				}

				if (_blendOverlapPercent == 0.0f)
				{
					// Create the cube render target
					int cubeDepth = 0;
					if (cubemapRenderMethod != CubemapRenderMethod.Manual)
					{
						cubeDepth = (int)_cubemapDepth;
					}
					int cubeAA = 1;
					if (cubemapRenderMethod != CubemapRenderMethod.Manual)
					{
						cubeAA = aaLevel;
					}
					if (_cubeTarget != null)
					{
						_cubeTarget.DiscardContents();
						if (_cubeTarget.width != cubemapResolution || 
							_cubeTarget.height != cubemapResolution || 
							_cubeTarget.depth != cubeDepth || 
							_cubeTarget.antiAliasing != cubeAA)
						{
							RenderTexture.Destroy(_cubeTarget);
							_cubeTarget = null;
						}
					}
					if (_cubeTarget == null)
					{
						_cubeTarget = new RenderTexture(cubemapResolution, cubemapResolution, cubeDepth, renderTextureFormat, RenderTextureReadWrite.Default);
						_cubeTarget.name = "[AVProMovieCapture] 360 Cube Target";
						_cubeTarget.isPowerOfTwo = true;
						_cubeTarget.dimension = UnityEngine.Rendering.TextureDimension.Cube;
						_cubeTarget.useMipMap = false;
						_cubeTarget.autoGenerateMips = false;
						_cubeTarget.antiAliasing = cubeAA;
						_cubeTarget.wrapMode = TextureWrapMode.Clamp;
						_cubeTarget.filterMode = FilterMode.Bilinear;
					}
				}

				if (_useMotionBlur)
				{
					_motionBlurCameras = new Camera[1];
					_motionBlurCameras[0] = _camera;
				}
			}

			_Transparency = Transparency.None;
			if ( !NativePlugin.IsBasicEdition() )
			{
				if( (_outputTarget == OutputTarget.VideoFile || _outputTarget == OutputTarget.NamedPipe) && GetEncoderHints().videoHints.transparency != Transparency.None )
				{
					_Transparency = GetEncoderHints().videoHints.transparency;
				}
				else if( _outputTarget == OutputTarget.ImageSequence && GetEncoderHints().imageHints.transparency != Transparency.None )
				{
					_Transparency = GetEncoderHints().imageHints.transparency;
				}
				//
				if( _Transparency == Transparency.TopBottom || _Transparency == Transparency.LeftRight )
				{
					InitialiseSideBySideTransparency( finalWidth, finalHeight );
				}

				switch ( _Transparency )
				{
					case Transparency.TopBottom:	finalHeight *= 2;	break;
					case Transparency.LeftRight:	finalWidth *= 2;	break;
				}
			}

			SelectRecordingResolution(finalWidth, finalHeight);

			GenerateFilename();

			if (base.PrepareCapture())
			{
				UpdateInjectionOptions(_stereoRendering, _render180Degrees?SphericalVideoLayout.Equirectangular180:SphericalVideoLayout.Equirectangular360);
				return true;
			}
			return false;
		}

		public override Texture GetPreviewTexture()
		{
			if (IsUsingMotionBlur())
			{
				return _motionBlur.FinalTexture;
			}
			return _finalTarget;
		}

		public override void Start()
		{
			Shader shader = Resources.Load<Shader>("CubemapToEquirectangular");
			if (shader != null)
			{
				_cubemapToEquirectangularMaterial = new Material(shader);
			}
			else
			{
				Debug.LogError("[AVProMovieCapture] Can't find CubemapToEquirectangular shader");
			}

			Shader blitShader = Shader.Find("Hidden/BlitCopy");
			if (blitShader != null)
			{
				_blitMaterial = new Material(blitShader);
			}
			else
			{
				Debug.LogError("[AVProMovieCapture] Can't find Hidden/BlitCopy shader");
			}
			_propFlipX = Shader.PropertyToID("_FlipX");
			#if SUPPORT_SHADER_ROTATION
			_propRotation = Shader.PropertyToID("_RotationMatrix");
			#endif

			base.Start();
		}

		public override void OnDestroy()
		{
			_targetNativePointer = System.IntPtr.Zero;

			if (_blitMaterial != null)
			{
				Material.Destroy(_blitMaterial);
				_blitMaterial = null;
			}

			if (_faceTarget != null)
			{
				RenderTexture.Destroy(_faceTarget);
				_faceTarget = null;
			}
			if (_faceTargets[0] != null)
			{
				for (int i = 0; i < 6; ++i)
				{
					RenderTexture.Destroy(_faceTargets[i]);
					_faceTargets[i] = null;
				}
			}
			if (_cubeTarget != null)
			{
				RenderTexture.Destroy(_cubeTarget);
				_cubeTarget = null;
			}
			if (_finalTarget != null)
			{
				RenderTexture.ReleaseTemporary(_finalTarget);
				_finalTarget = null;
			}
			base.OnDestroy();
		}
	}
}