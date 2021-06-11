using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;
using DlibFaceLandmarkDetector;
using Assets.DlibFaceLandmarkDetectorWithOpenCVExample;

using PupilDetectionDLL;        // pupil detection dll
using FERdll;                   // facial expression dll
using UnityEngine.UI;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// Face Landmark Detection from WebCamTextureToMat Example.
    /// </summary>

    [RequireComponent(typeof(LineRenderer))] // LineRenderer 컴포넌트추가
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class WebCamTextureToMatExample : MonoBehaviour
    {
        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The web cam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;

        /// <summary>
        /// The cam matrix.
        /// </summary>
        Mat camMatrix;

        /// <summary>
        /// The dist coeffs.
        /// </summary>
        MatOfDouble distCoeffs;

        /// <summary>
        /// The 3d face object points.
        /// </summary>
        MatOfPoint3f objectPoints;

        /// <summary>
        /// The image points.
        /// </summary>
        MatOfPoint2f imagePoints;

        /// <summary>
        /// The rvec.
        /// </summary>
        Mat rvec;

        /// <summary>
        /// The tvec.
        /// </summary>
        Mat tvec;

        /// <summary>
        /// pupil detector
        /// </summary>
        PupilDetector pupilDtc;

        /// <summary>
        /// facial emotion detector
        /// </summary>
        FacialExpression ferDtc = null;

        /// <summary>
        /// emotion detection processing for the facial emotion
        /// </summary>
        EmotionDetection emotionProc;

        /// <summary>
        /// The shape_predictor_68_face_landmarks_dat_filepath.
        /// </summary>
        private string shape_predictor_68_face_landmarks_dat_filepath;

        /// <summary>
        /// the number of class for running  (현재 프로그램에서 사용하고자 하는 클래스 수, 3 or 7로 설정)
        /// </summary>
        static int class_cnt = 7;                   // 7 class

        private bool bTrainRF;                      // classifier 학습 데이터 로드 유무

        int emotionResult = -1;                     // result of facial emotion
        int prev_emotion = 0;
        int frame_5count = 0;
        int loaded_class_cnt = 1;                   // the number of class

        PPoint[] landmarks = new PPoint[68];        // extracted facial landmark 

        PPoint prev_pupil_pl;                       // left pupil position of previous frame  (relative coordinate from positions of eye top & left)
        PPoint prev_pupil_pr;                       // right pupil position of previous frame (relative coordinate from positions of eye top & left)

        List<Vector2> prev_landmarks = null;       // landmarks of previous frame









        // public 변수
        static public bool trigger; // 이벤트 트리거 0 - 게임정지, 1 - 게임 시작
        static public bool canShoot; //미사일를 쏠 수 있는 상태인지 검사
        public bool isRayShoot; // Ray가 발사 되는가
        public GameObject Rocket07_Red; //발사할 미사일을 저장
        public Text countDown;
        public GameObject playBoard;

        // private 변수
        private Vector3 rightEye, rightCenterPos; // 오른쪽 동공 위치, 오른쪽 눈 정중앙
        private Vector3 rightDirSeeing, rightNextPos; // 현재 보고있는 방향벡터, 다음 위치값
        private Vector3 leftEye, leftCenterPos; // 왼쪽 동공 위치, 왼쪽 눈 정중앙
        private Vector3 leftDirSeeing, leftNextPos; // 현재 보고있는 방향벡터, 다음 위치값

        private float shootDelay; //미사일를 쏘는 주기
        private float shootTimer; //시간을 잴 타이머
        private float nextPosRange; // 레이저 또는 미사일을 얼마나 멀리 보낼지 정함
        private GameObject instanceMissile; // 실제로 미사일오브젝트를 복사해서 할당할 오브젝트 계속 Destroy, Instantiate 를 해주기위해 따로 선언

        private Ray rightShootRay; // 레이캐스트
        private Ray LeftShootRay; // 레이캐스트
        private RaycastHit[] rightShootHits; // 레이케스트 히트된 것들 (오른쪽 눈)
        private RaycastHit[] leftShootHits; // 레이케스트 히트된 것들 (왼쪽 눈)
        public LineRenderer rightLineRenderer; // 라인렌더러
        public LineRenderer leftLineRenderer; // 라인렌더러
        private readonly CapsuleCollider capsule; // 라인 렌더러에 붙힐 콜라이더
        private int missileType; // 0 - missile, 1 - raser
        private bool displayReady; // 3,2,1 화면에 띄우기

        // 초기설정 되돌아가기 위한 변수선언
        private Transform initTransform; // 초기에 설정된 Transform 좌표 저장
        private int initWidth, initHeight; // 초기 설정된 Request Width와 Height 저장
        private float settingPosX, settingPosY;
        private bool settingGame;

        IEnumerator DisplayReady()
        {
            int i = 0;
            while (i <= 3)
            {
                countDown.enabled = false;
                yield return new WaitForSeconds(0.5f);
                countDown.enabled = true;
                countDown.text = (3 - i).ToString();
                yield return new WaitForSeconds(0.5f);
                i++;
            }
            if (i > 3)
            {
                countDown.text = "Go!";
                yield return new WaitForSeconds(1.0f);
                countDown.enabled = false;
                canShoot = true;
            }
            UIManager.S.ChangeUIState();
        }

        private void InitGame() // 시작 전 초기값 전달 -> start()
        {
            // Vector3 메모리 할당
            rightEye = new Vector3();
            rightCenterPos = new Vector3();
            leftEye = new Vector3();
            leftCenterPos = new Vector3();
            rightDirSeeing = new Vector3();
            rightNextPos = new Vector3();
            leftDirSeeing = new Vector3();
            leftNextPos = new Vector3();

            // 초깃값 전달
            canShoot = false;
            shootDelay = 1.8f;
            shootTimer = 0;
            nextPosRange = 100.0f;
            trigger = false;
            isRayShoot = false;
            missileType = 0;
            displayReady = false;
            countDown.enabled = false;

            // LineRenderer 초기 설정
            rightLineRenderer.enabled = false;
            rightLineRenderer.startWidth = 1.0f;
            rightLineRenderer.endWidth = 1.0f;
            rightLineRenderer.startColor = Color.yellow;
            rightLineRenderer.endColor = Color.red;
            rightLineRenderer.useWorldSpace = false;
            leftLineRenderer.enabled = false;
            leftLineRenderer.startWidth = 1.0f;
            leftLineRenderer.endWidth = 1.0f;
            leftLineRenderer.startColor = Color.yellow;
            leftLineRenderer.endColor = Color.red;
            leftLineRenderer.useWorldSpace = false;

            LeftShootRay = new Ray();
            rightShootRay = new Ray();

            // 게임 종료할때 돌아갈 Quad 저장
            initTransform = this.transform;
            initWidth = webCamTextureToMatHelper.requestWidth;
            initHeight = webCamTextureToMatHelper.requestHeight;
            settingGame = false;
            playBoard.SetActive(false);
            settingPosX = playBoard.GetComponent<RectTransform>().anchoredPosition.x;
            settingPosY = playBoard.GetComponent<RectTransform>().position.y - (playBoard.GetComponent<RectTransform>().localScale.y / 2);

            //20.01.07
            Screen.sleepTimeout = SleepTimeout.NeverSleep; // 게임중 일땐 화면이 꺼지지 않게 함
        }

        private void SetVectors(PPoint leye, PPoint reye, PPoint right_center, PPoint left_center)
        {
            // 오른쪽
            // 동공 위치 셋팅
            rightEye.x = reye.x + this.transform.localPosition.x - (webCamTextureToMatHelper.requestWidth / 2); // Quad position.x - 640/2 + reye.x = 553.0f+reye.x
            rightEye.y = this.transform.localPosition.y + (webCamTextureToMatHelper.requestHeight / 2) - reye.y; // Quad position.y - 480/2 - reye.y = 584.0f - reye.y
            rightEye.z = this.transform.position.z;
            // 눈 중앙 위치 셋팅
            rightCenterPos.x = right_center.x + this.transform.localPosition.x - (webCamTextureToMatHelper.requestWidth / 2);
            rightCenterPos.y = this.transform.localPosition.y + (webCamTextureToMatHelper.requestHeight / 2) - right_center.y;
            rightCenterPos.z = this.transform.position.z;
            // 현재 보고있는 방향 셋팅
            rightDirSeeing = rightEye - rightCenterPos;
            // 다음 위치 셋팅
            rightNextPos = rightCenterPos + (rightDirSeeing * nextPosRange);
            rightNextPos.z = this.transform.position.z-1;

            // 왼쪽
            // 동공 위치 셋팅
            leftEye.x = leye.x + this.transform.localPosition.x - (webCamTextureToMatHelper.requestWidth / 2); // Quad position.x - 640/2 + reye.x = 553.0f+reye.x
            leftEye.y = this.transform.localPosition.y + (webCamTextureToMatHelper.requestHeight / 2) - leye.y; // Quad position.y - 480/2 - reye.y = 584.0f - reye.y
            leftEye.z = this.transform.position.z;
            // 눈 중앙 위치 셋팅
            leftCenterPos.x = left_center.x + this.transform.localPosition.x - (webCamTextureToMatHelper.requestWidth / 2);
            leftCenterPos.y = this.transform.localPosition.y + (webCamTextureToMatHelper.requestHeight / 2) - left_center.y;
            leftCenterPos.z = this.transform.position.z;
            // 현재 보고있는 방향 셋팅
            leftDirSeeing = leftEye - leftCenterPos;
            // 다음 위치 셋팅
            leftNextPos = leftCenterPos + (leftDirSeeing * nextPosRange);
            leftNextPos.z = this.transform.position.z-1;
        }

        private void Shooting() // 발사
        {
            if (canShoot)
            {
                switch (missileType)
                {
                    case 0:
                        StopRaser();
                        ShootMissile();
                        break;
                    case 1:
                        StartRaser();
                        if (isRayShoot)
                            ShootRaser();
                        break;
                }
            }
        }

        void ShootMissile() // 발사를 관리하는 함수
        {
            if (shootTimer > shootDelay) //쿨타임이 지났는지 검사
            {
                // 미사일 객체 생성
                instanceMissile = Instantiate(Rocket07_Red);
                instanceMissile.transform.parent = GameObject.Find("Shooting").transform;
                instanceMissile.transform.localPosition = rightCenterPos;
                instanceMissile.GetComponent<Missile>().moveDir = rightDirSeeing;
                instanceMissile.transform.localScale = new Vector3(8.0f,8.0f,8.0f);

                instanceMissile = Instantiate(Rocket07_Red);//레이저를 생성
                instanceMissile.transform.parent = GameObject.Find("Shooting").transform;
                instanceMissile.transform.localPosition = leftCenterPos;
                instanceMissile.GetComponent<Missile>().moveDir = leftDirSeeing;
                instanceMissile.transform.localScale = new Vector3(8.0f, 8.0f, 8.0f);
                shootTimer = 0; //쿨타임을 다시 카운트
            }
            shootTimer += Time.deltaTime; //쿨타임을 카운트
        }

        void StartRaser()
        {
            rightLineRenderer.enabled = true;
            rightLineRenderer.positionCount = 2;
            leftLineRenderer.enabled = true;
            leftLineRenderer.positionCount = 2;
            isRayShoot = true;
        }

        void StopRaser()
        {
            isRayShoot = false;
            rightLineRenderer.positionCount = 0;
            rightLineRenderer.enabled = false;
            leftLineRenderer.positionCount = 0;
            leftLineRenderer.enabled = false;
        }

        public Transform tempT;
        public Vector3 GlobalToLocalTransVector(Vector3 v)
        {
            //0기준으로 벡터 크기가 글로벌 크기로 얼마인지를 반환 ..
            tempT.position = v;
            Vector3 swap = tempT.localPosition;
            swap.z = 0.0f;
            tempT.localPosition = swap;
            return tempT.localPosition;
        }

        public Vector3 LocalToGlobalTransVector(Vector3 v)
        {
            //0기준으로 벡터 크기가 글로벌 크기로 얼마인지를 반환 ..
            tempT.localPosition = v;
            Vector3 swap = tempT.localPosition;
            swap.z = 0.0f;
            tempT.localPosition = swap;
            return tempT.position;
        }

        double distanceRay(Vector3 a, Vector3 b)
        {
            double x = Math.Exp(a.x - b.x);
            double y = Math.Exp(a.y - b.y);
            double distance = Math.Sqrt(x+y);
            return distance;
        }
        
        // LineRenderer, Ray - Shoot
        void ShootRaser()
        {
            Vector3 rightLineFirst = new Vector3((rightCenterPos.x - transform.localPosition.x) / webCamTextureToMatHelper.requestWidth, (rightCenterPos.y - transform.localPosition.y) / webCamTextureToMatHelper.requestHeight, -1);
            rightLineRenderer.SetPosition(0, rightLineFirst);// lineRenderer 0번째 설정
            rightLineRenderer.SetPosition(1, rightNextPos); // lineRenderer 1번째 설정
            rightShootRay.origin = LocalToGlobalTransVector(rightLineFirst);
            rightShootRay.direction = (Vector2)rightNextPos * nextPosRange;
            //Debug.DrawRay(rightShootRay.origin, (Vector2)rightNextPos * nextPosRange, Color.black, 1.0f, true);
            rightShootHits = Physics.RaycastAll(rightShootRay.origin, (Vector2)rightNextPos * nextPosRange, Mathf.Infinity);


            Vector3 leftLineFirst = new Vector3((leftCenterPos.x - transform.localPosition.x) / webCamTextureToMatHelper.requestWidth, (leftCenterPos.y - transform.localPosition.y) / webCamTextureToMatHelper.requestHeight, -1);
            leftLineRenderer.SetPosition(0, leftLineFirst);// lineRenderer 0번째 설정
            leftLineRenderer.SetPosition(1, leftNextPos); // lineRenderer 1번째 설정
            LeftShootRay.origin = LocalToGlobalTransVector(leftLineFirst);
            //LeftShootRay.direction = LocalToGlobalTransVector(leftNextPos);
            LeftShootRay.direction = (Vector2)leftNextPos * nextPosRange;
            //Debug.DrawRay(LeftShootRay.origin, (Vector2)leftNextPos * nextPosRange, Color.black, 1.0f, true);
            leftShootHits = Physics.RaycastAll(rightShootRay.origin, (Vector2)leftNextPos * nextPosRange, Mathf.Infinity);


            //leftShootHits = Physics.RaycastAll(LeftShootRay.origin, LeftShootRay.direction, Mathf.Infinity);

            foreach (var hit in rightShootHits)
            {
                if (hit.collider.gameObject.tag == "Enemy")
                {
                    Debug.DrawRay(rightShootRay.origin, (Vector2)rightNextPos * nextPosRange, Color.blue, 5.0f);
                    gameManager.instance.AddScore(50);
                    //OnPauseButton();
                    Destroy(hit.collider.gameObject);
                }
            }

            foreach (var hit in leftShootHits)
            {
                if (hit.collider.gameObject.tag == "Enemy")
                {
                    //Debug.DrawRay(LeftShootRay.origin, hit.collider.gameObject.transform.localPosition, Color.blue, 5.0f);
                    Debug.DrawRay(LeftShootRay.origin, (Vector2)leftNextPos * nextPosRange, Color.blue, 5.0f);
                    gameManager.instance.AddScore(50);
                    //OnPauseButton();
                    Destroy(hit.collider.gameObject);
                }
            }

            /*
            lineRenderer.SetPosition(0, rightCenterPos);
            lineRenderer.SetPosition(1, rightNextPos); // lineRenderer 1번째 설정
            shootRay.origin = rightCenterPos;
            shootRay.direction = rightNextPos;

            Debug.DrawLine(rightCenterPos, shootHit.point, Color.yellow);
            // is ray target
            if (Physics.Raycast (shootRay, out shootHit, Mathf.Infinity))
            {
                Vector3 v3Pos = shootRay.GetPoint(shootHit.distance * 0.995f);
                //lineRenderer.SetPosition(1, rightNextPos); // lineRenderer 1번째 설정
                //lineRenderer.SetPosition(1, v3Pos); // lineRenderer 1번째 target position 설정

                // check ray tag 
                if (shootHit.collider.gameObject.tag == "Enemy")
                {
                    gameManager.instance.AddScore(50);
                    Destroy(shootHit.collider.gameObject);
                }
            }
            /*
            // is ray target null
            else
            {
                lineRenderer.SetPosition(1, rightNextPos); // lineRenderer 1번째 설정
            }
            */
        }

        public void OnGameStart()
        {
            if (trigger)
            {
                trigger = false;
                displayReady = false;
            }
            else
            {
                trigger = true;
                displayReady = true;
            }
        }

        public void OnMissileButton()
        {
            if (trigger)
            {
                missileType = 0;
            }
        }


        public void OnRaserButton()
        {
            if (trigger)
            {
                missileType = 1;
            }
        }
        // end













        // Use this for initialization
        void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            StartCoroutine(DlibFaceLandmarkDetector.Utils.getFilePathAsync("shape_predictor_68_face_landmarks.dat", (result) => {
                shape_predictor_68_face_landmarks_dat_filepath = result;
                Run ();
            }));
#else
            shape_predictor_68_face_landmarks_dat_filepath = DlibFaceLandmarkDetector.Utils.getFilePath("shape_predictor_68_face_landmarks.dat");


            Run();
#endif

            bTrainRF = false;

            InitGame(); // 초기 게임설정
        }

        private void Run()
        {
            // Dlib의 FaceLandmarkDetector 초기화
            faceLandmarkDetector = new FaceLandmarkDetector(shape_predictor_68_face_landmarks_dat_filepath);

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();
            webCamTextureToMatHelper.Init();

            // 각종 변수 초기화
            for (int l = 0; l < 68; l++)
            {
                landmarks[l] = new PPoint();
            }

            prev_pupil_pl = new PPoint();
            prev_pupil_pr = new PPoint();
            prev_pupil_pl.x = -9999;
            prev_pupil_pl.y = -9999;
            prev_pupil_pr.x = -9999;
            prev_pupil_pr.y = -9999;

            pupilDtc = new PupilDetector();       // init pupil detector            
            emotionProc = new EmotionDetection();     // init emotion processing            
        }

        /// <summary>
        /// Raises the web cam texture to mat helper inited event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInited()
        {
            Debug.Log("OnWebCamTextureToMatHelperInited");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");
        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update()
        {
            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = webCamTextureToMatHelper.GetMat();                // load image of current frame    (type: 16)                                                                            

                Mat grayMat = new Mat();
                Imgproc.cvtColor(rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
                Scalar means = Core.mean(grayMat);

                if (means.val[0] < 80 &&
                    (webCamTextureToMatHelper.requestIsFrontFacing && webCamTextureToMatHelper.GetWebCamTexture().requestedFPS == 0))
                {
                    string fps;
                    fps = webCamTextureToMatHelper.GetWebCamTexture().requestedFPS.ToString() + "f";
                    emotionProc.DrawText(rgbaMat, fps, new Point(10, 30), Core.FONT_HERSHEY_COMPLEX, 1.0, new Scalar(0, 0, 255, 255), 1);
                    webCamTextureToMatHelper.Stop();
                    webCamTextureToMatHelper.GetWebCamTexture().requestedFPS = 15;
                    webCamTextureToMatHelper.Play();
                    return;
                }

                Imgproc.equalizeHist(grayMat, grayMat);

                //Imgproc.cvtColor(grayMat, rgbaMat, Imgproc.COLOR_GRAY2RGBA);

                // color image -> gray image
                /*Mat YCrCbMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), rgbaMat.type());
                List<Mat> lYCrCB = new List<Mat>(3);             

                Imgproc.cvtColor(rgbaMat, YCrCbMat, Imgproc.COLOR_RGB2YCrCb);       
                OpenCVForUnity.Core.split(YCrCbMat, lYCrCB);
               // Imgproc.equalizeHist(lYCrCB[0], lYCrCB[0]);                         // apply Hitogram Equalization 2019-12-13
                OpenCVForUnity.Core.merge(lYCrCB, YCrCbMat);
                Imgproc.cvtColor(YCrCbMat, rgbaMat, Imgproc.COLOR_YCrCb2RGB);
                */
                OpenCVForUnityUtils.SetImage(faceLandmarkDetector, grayMat);    // transfer image for detection of facial landmark

                /////////////////////////////////////////////////////////
                //detect face rects
                /////////////////////////////////////////////////////////
                List<UnityEngine.Rect> detectResult = faceLandmarkDetector.Detect();

                if (detectResult.Count < 1)
                {
                    // No face                 
                    emotionProc.DrawText(rgbaMat, "No Face",
                        new Point((double)(rgbaMat.width()), (double)(rgbaMat.height())),
                        Core.FONT_HERSHEY_SIMPLEX,
                        0.4,
                        new Scalar(0, 0, 255, 255),
                        1);
                }
                else
                {
                    foreach (var rect in detectResult)
                    {
                        /////////////////////////////////////////////////////////
                        //detect landmark points
                        /////////////////////////////////////////////////////////
                        List<Vector2> points = faceLandmarkDetector.DetectLandmark(rect);

                        for (int l = 0; l < 68; l++)
                        {
                            landmarks[l].x = (int)points[l].x;
                            landmarks[l].y = (int)points[l].y;
                        }

                        ///////////////////////////
                        // detect pupil
                        ///////////////////////////       
                        PPoint pupil_lp = new PPoint();         // position of left pupil
                        PPoint pupil_rp = new PPoint();         // position of right pupil
                        int radius_l = 0;                       // radius of left eye 
                        int radius_r = 0;                       // radius of right eye   

                        // color image -> gray image
                        //Mat grayimg = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC1);
                        //Imgproc.cvtColor(rgbaMat, grayimg, Imgproc.COLOR_RGB2GRAY);

                        // mat format -> byte array
                        byte[] img_array = new byte[grayMat.rows() * grayMat.cols()];
                        OpenCVForUnity.Utils.copyFromMat(grayMat, img_array);

                        // parameters
                        // img : byte array of gray image
                        // landmarks : facial landmark points (68 landmarks)
                        // pupil_pl : result of the left pupil position 
                        // pupil_pr : result of the right pupil position 
                        // radius_l : result of the left pupil radius
                        // radius_r : result of the right pupil radius
                        // prev_pupil_pl : the left pupil position of previous frame
                        // prev_pupil_pr : the right pupil position of previous frame
                        // note : if the pupil position value is less than zero, the current eye is closed eye or bad detection result.
                        pupilDtc.Detect_pupil(img_array, grayMat.width(), grayMat.height(), landmarks, ref pupil_lp, ref pupil_rp, ref radius_l, ref radius_r, ref prev_pupil_pl, ref prev_pupil_pr);


                        if (bTrainRF)
                        {
                            // draw landmark points -> 모든 landmark points 를 점, text를 추가함
                            emotionProc.DrawFaceLandmark(rgbaMat, points, new Scalar(0, 0, 255, 255), 1);

                            // 표정 변화가 있을 경우만 표정 추출
                            // get landmark_variance between current and previous frame.                                
                            double landVariance = 0.0;
                            if (++frame_5count > 4)
                            {
                                //if( prev_landmarks != null )
                                landVariance = ferDtc.GetVarianceLandmarkFromPrev(prev_landmarks, points);
                                frame_5count = 0;
                                prev_landmarks = points;
                            }

                            ///////////////////////////////////////////////////////////
                            // detect facial emotion
                            //////////////////////////////////////////////////////////
                            if (landVariance > 0.3 || (emotionResult == 0 && landVariance > 0.2))
                            {
                                if (bTrainRF)
                                {
                                    emotionResult = ferDtc.extractFER_7class(points);

                                    if (emotionResult == -1)
                                    {
                                        Debug.Log("[Error] You must load all random forest classifier files.");
                                        return;
                                    }
                                    if (emotionResult == -3)
                                    {
                                        Debug.Log("[Error] The number of feature deimension is wrong. check the rf files.");
                                        return;
                                    }

                                    prev_emotion = emotionResult;
                                }
                            }
                            else
                            {
                                emotionResult = prev_emotion;
                            }

                            // draw emotion result
                            if (emotionResult >= 0)
                            {
                                emotionProc.DrawEmotion(rgbaMat, rect, loaded_class_cnt, emotionResult);
                            }
                        }
                        // draw pupil info -> 동공 영역
                        emotionProc.DrawPupils(rgbaMat, pupil_lp, pupil_rp, radius_l, radius_r, new Scalar(255, 0, 0, 255), new Scalar(0, 255, 0, 255));
















                        ///////////////////////////
                        ////Game
                        ///////////////////////////

                        if (detectResult.Count > 0)
                        {
                            // 눈의 중간좌표 찾기
                            //Debug.Log(points[39].x);
                            PPoint leftCenterEye = new PPoint();
                            leftCenterEye.x = (int)((points[39].x + points[36].x) / 2.0f);
                            leftCenterEye.y = (int)((points[39].y + points[36].y) / 2.0f);

                            PPoint rightCenterEye = new PPoint();
                            rightCenterEye.x = (int)((points[45].x + points[42].x) / 2.0f);
                            rightCenterEye.y = (int)((points[45].y + points[42].y) / 2.0f);
                            if (trigger) // Game Start
                            {
                                if (!settingGame)
                                {
                                    settingGame = true;
                                    GameObject.Find("GameStartButton").GetComponentInChildren<Text>().text = "STOP"; // 버튼 Text 변경
                                    playBoard.SetActive(true); // PlayBoard 활성화     

                                    // requestHeight, width 변경 (속도 때문에 줄이는 경우)
                                    webCamTextureToMatHelper.requestHeight = 240;
                                    webCamTextureToMatHelper.requestWidth = 320;
                                    //this.transform.localPosition = new Vector3(settingPosX, settingPosY-Screen.height/2 + webCamTextureToMatHelper.requestHeight, 0.0f);
                                    this.transform.localPosition = new Vector3(settingPosX, settingPosY - 56.0f);
                                    OnWebCamTextureToMatHelperInited();
                                    webCamTextureToMatHelper.Init(null, webCamTextureToMatHelper.requestWidth, webCamTextureToMatHelper.requestHeight, !webCamTextureToMatHelper.requestIsFrontFacing);
                                    return;
                                }


                                if (displayReady && !canShoot)
                                {
                                    StartCoroutine(DisplayReady());// 3,2,1 화면에 띄우고 canShoot = true
                                    displayReady = false;
                                }
                                SetVectors(pupil_lp, pupil_rp, rightCenterEye, leftCenterEye); // 방향 벡터 설정
                                Shooting(); // 슈팅시작
                            }
                            else // Game End & OverGame
                            {
                                // 레이저 발사정지
                                StopRaser();
                                canShoot = false;

                                if (settingGame)
                                {
                                    settingGame = false;

                                    // 게임보드 비활성화
                                    playBoard.SetActive(false);

                                    // Qaud 상태 되돌리기
                                    this.transform.position = initTransform.position;
                                    webCamTextureToMatHelper.requestHeight = initHeight;
                                    webCamTextureToMatHelper.requestWidth = initWidth;
                                    GameObject.Find("GameStartButton").GetComponentInChildren<Text>().text = "GameStart";
                                    webCamTextureToMatHelper.Init(null, webCamTextureToMatHelper.requestWidth, webCamTextureToMatHelper.requestHeight, !webCamTextureToMatHelper.requestIsFrontFacing);
                                    this.transform.localPosition = new Vector3(playBoard.GetComponent<RectTransform>().position.x, playBoard.GetComponent<RectTransform>().position.y, 0);
                                    return;
                                }
                            }

                        }
                        else
                        {
                            //////////////////////////
                            //draw 
                            //////////////////////////

                            // draw pupil info -> 동공 영역
                            emotionProc.DrawPupils(rgbaMat,
                                pupil_lp, pupil_rp,
                                radius_l, radius_r,
                                new Scalar(255, 0, 0, 255), new Scalar(0, 255, 0, 255));



                            // 중앙 미간라인 표시
                            PPoint eyes = new PPoint();
                            eyes.x = (int)points[27].x;
                            eyes.y = (int)points[27].y;
                            emotionProc.DrawPupils(rgbaMat, eyes, eyes, radius_l, radius_r, new Scalar(255, 0, 0, 255), new Scalar(255, 0, 0, 255));


                            /*
                            //added 2019.12.24 -> 눈 시선 움직이는 라인생성
                            MatOfPoint3f nose_tip_point3D = new MatOfPoint3f(new Point3(0, 40, 300.0));
                            MatOfPoint2f nose_tip_point2D = new MatOfPoint2f();
                            Point pupilLPoint = new OpenCVForUnity.Point(0, 0);
                            Point eyeLCenterPoint = new OpenCVForUnity.Point(0, 0);
                            Point pupilRPoint = new OpenCVForUnity.Point(0, 0);
                            Point eyeRCenterPoint = new OpenCVForUnity.Point(0, 0);
                            int eyeballL_z = 18;
                            int eyeballR_z = 18;
                            double disErrorL = 0.0;
                            double disErrorR = 0.0;
                            //draw head directioni line
                            Point[] pArr = nose_tip_point2D.toArray();
                            //Imgproc.line(rgbaMat, new Point(points[33].x, points[33].y), pArr[0], new Scalar(255, 0, 0, 255), 2);

                            // right pupil
                            if (Mathf.Abs((points[44].y - points[46].y)) > Mathf.Abs((points[42].x - points[45].x)) * 0.25)  // 6.0->4.0
                            {
                                pupilRPoint = OpenCVForUnityUtils.detect_pupil(rgbaMat, points, 1);
                                eyeRCenterPoint.x = (int)((points[42].x + points[45].x) / 2.0f);
                                //eyeCenterPoint.y = (int)((points[42].y + points[45].y) / 2.0f) - Math.Abs(points[47].x - points[46].x) * 0.1;
                                eyeRCenterPoint.y = (int)((points[44].y + points[47].y) / 2.0f);
                                eyeballR_z = (int)(Math.Abs(points[42].x - points[45].x) * 1.2);  // 0.9 default 18
                                if (rect.width < 100)
                                    disErrorR = Math.Abs(points[46].x - points[47].x) * 0.4;
                                else
                                    disErrorR = -1.0;
                            }

                            // left pupil
                            if (Mathf.Abs((points[37].y - points[41].y)) > Mathf.Abs((points[39].x - points[36].x)) * 0.25)
                            {
                                pupilLPoint = OpenCVForUnityUtils.detect_pupil(rgbaMat, points, 0);
                                eyeLCenterPoint.x = (int)((points[36].x + points[39].x) / 2.0f);
                                //eyeCenterPoint.y = (int)((points[36].y + points[39].y) / 2.0f) - Math.Abs(points[41].x - points[40].x) * 0.1;
                                eyeLCenterPoint.y = (int)((points[38].y + points[41].y) / 2.0f);
                                eyeballL_z = (int)(Math.Abs(points[36].x - points[39].x) * 1.2);  // 0.9
                                if (rect.width < 100)
                                    disErrorL = Math.Abs(points[40].x - points[41].x) * 0.4;
                                else
                                    disErrorL = -1.0;
                            }

                            //draw gaze line
                            if (pupilLPoint.x != 0)
                            {
                                Imgproc.circle(rgbaMat, pupilLPoint, 2, new Scalar(255, 0, 0, 255), 1);               // draw pupil
                                OpenCVForUnityUtils.draw_gazeLine(rgbaMat, eyeLCenterPoint, pupilLPoint, disErrorL, eyeballL_z, 200);      // draw left gaze Line
                            }
                            if (pupilRPoint.x != 0)
                            {
                                Imgproc.circle(rgbaMat, pupilRPoint, 2, new Scalar(255, 0, 0, 255), 1);               // draw pupil
                                OpenCVForUnityUtils.draw_gazeLine(rgbaMat, eyeRCenterPoint, pupilRPoint, disErrorR, eyeballR_z, 200);      // draw right gaze Line              

                            }*/
                        }
                    }

                }

                OpenCVForUnity.Utils.matToTexture2D(rgbaMat, texture, webCamTextureToMatHelper.GetBufferColors());
            }
        }

        /// <summary>
        /// Raises the disable event.
        /// </summary>
        void OnDisable()
        {
            if (webCamTextureToMatHelper != null)
                webCamTextureToMatHelper.Dispose();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose();
        }

        /// <summary>
        /// Raises the back button event.
        /// </summary>
        public void OnBackButton()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene("DlibFaceLandmarkDetectorExample");
            #else
            Application.LoadLevel ("DlibFaceLandmarkDetectorExample");
            #endif
        }

        /// <summary>
        /// Raises the play button event.
        /// </summary>
        public void OnPlayButton()
        {
            webCamTextureToMatHelper.Play();
            UIManager.S.ChangeUIState();
            gameManager.instance.isPause = false;
        }

        /// <summary>
        /// Raises the pause button event.
        /// </summary>
        public void OnPauseButton()
        {
            webCamTextureToMatHelper.Pause();
            UIManager.S.ChangeUIState();
            gameManager.instance.isPause = true;
        }

        /// <summary>
        /// Raises the stop button event.
        /// </summary>
        public void OnStopButton()
        {
            webCamTextureToMatHelper.Stop();
        }

        public void OnExitButton()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }

        /// <summary>
        /// Raises the change camera button event.
        /// </summary>
        public void OnChangeCameraButton()
        {
            webCamTextureToMatHelper.Init(null, webCamTextureToMatHelper.requestWidth, webCamTextureToMatHelper.requestHeight, !webCamTextureToMatHelper.requestIsFrontFacing);

        }

        /// <summary>OnTrainSVMButton
        /// Raises the train svm button event.
        /// </summary>
        public void OnTrainSVMButton()
        {
            if (!bTrainRF)
            {
                // for class 7
                string str_rf_file_path_a = DlibFaceLandmarkDetector.Utils.getFilePath("randomforest_tree_a.json");
                string str_rf_file_path_d = DlibFaceLandmarkDetector.Utils.getFilePath("randomforest_tree_d.json");
                string str_rf_file_path_sub_a = DlibFaceLandmarkDetector.Utils.getFilePath("randomforest_tree_sub_a.json");
                string str_rf_file_path_sub_d = DlibFaceLandmarkDetector.Utils.getFilePath("randomforest_tree_sub_d.json");

                ferDtc = new FacialExpression(str_rf_file_path_a, str_rf_file_path_d, str_rf_file_path_sub_a, str_rf_file_path_sub_d);          // init facial emotion detector
                loaded_class_cnt = ferDtc.GetClassCount();

                Debug.Log("Loaded RF clssifier files. (class# " + loaded_class_cnt + ")");
                bTrainRF = true;
            }
            else
            {
                bTrainRF = false;
            }

        }
    }
}