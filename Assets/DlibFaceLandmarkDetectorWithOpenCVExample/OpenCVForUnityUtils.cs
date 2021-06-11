using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using OpenCVForUnity;
using DlibFaceLandmarkDetector;
using Assets.DlibFaceLandmarkDetectorWithOpenCVExample;

namespace DlibFaceLandmarkDetector
{    

    /// <summary>
    /// Utility class for the integration of DlibFaceLandmarkDetector and OpenCVForUnity.
    /// </summary>
    public static class OpenCVForUnityUtils
    {
        /// <summary>
        /// Sets the image.
        /// </summary>
        /// <param name="faceLandmarkDetector">Face landmark detector.</param>
        /// <param name="imgMat">Image mat.</param>
        public static void SetImage (FaceLandmarkDetector faceLandmarkDetector, Mat imgMat)
        {
            if (!imgMat.isContinuous ()) {
                throw new ArgumentException ("imgMat.isContinuous() must be true.");
            }
            faceLandmarkDetector.SetImage ((IntPtr)imgMat.dataAddr (), imgMat.width (), imgMat.height (), (int)imgMat.elemSize ());
        }

        /** added 2019.12.24
            @brief          Draw gaze line using pupil points with eyeball model
            @param          imgMat : original image
                            pupilLpoint : Left pupil point
                            distError : 눈 중심에서 오차 거리
                            eyeball_z : eyeball의 중심에서 pupil까지의 거리
                            display_z : display를 위한 eyeball 중심에서 출력 z까지의 거리
         */

        public static double getDistance(Point p1, Point p2)
        {
            return Math.Sqrt((p1.x - p2.x) * (p1.x - p2.x) + (p1.y - p2.y) * (p1.y - p2.y));
        }

        /**
           @brief      pupil detection using the FRST algorithm ( input of FRST : eye region )
           @param      imgMat : original image
                       points : 68 landmarks
                       mode : 0:left, 1:right
        */

        // for FRST
        const int FRST_MODE_BRIGHT = 1;
        const int FRST_MODE_DARK = 2;
        const int FRST_MODE_BOTH = 3;

        public static Point detect_pupil(Mat imgMat, List<Vector2> points, int mode)
        {
            int eye_gap, eye_left, eye_top, eye_bottom, eye_w, eye_h;
            int radii_start, radii_end;  // for FRST
            //Point eyeCenterR = new Point(0, 0);
            if (mode == 0)     // left eye
            {
                eye_gap = 0;//(int)(Math.Abs(points[41].x - points[40].x) * 0.2);
                eye_left = (int)points[36].x + eye_gap;
                eye_top = (int)((points[37].y + points[38].y) / 2.0f) + eye_gap;
                eye_bottom = (int)((points[40].y + points[41].y) / 2.0f);
                eye_w = (int)Math.Abs(points[39].x - points[36].x) - eye_gap * 2;
                eye_h = (int)Math.Abs(eye_bottom - eye_top) - eye_gap * 2;

                radii_start = (int)(Math.Abs(points[41].x - points[40].x) * 0.7);  // default : 3 : 0.7 0.5
                radii_end = (int)(Math.Abs(points[41].x - points[40].x) * 1.3);       // default : 6  : 1.2 1.2 

                //eyeCenterR.x = Math.Abs(points[36].x + points[39].x) / 2;
                //eyeCenterR.y = Math.Abs(points[36].y + points[39].y) / 2 - Math.Abs(points[41].x - points[40].x) * 0.1;
            }
            else
            {
                eye_gap = 0;// (int)(Math.Abs(points[47].x - points[46].x) * 0.2);
                eye_left = (int)points[42].x + eye_gap;
                eye_top = (int)((points[43].y + points[44].y) / 2.0f) + eye_gap;
                eye_bottom = (int)((points[46].y + points[47].y) / 2.0f);
                eye_w = (int)Math.Abs(points[45].x - points[42].x) - eye_gap * 2;
                eye_h = (int)Math.Abs(eye_bottom - eye_top) - eye_gap * 2;

                radii_start = (int)(Math.Abs(points[47].x - points[46].x) * 0.7);  // default : 3
                radii_end = (int)(Math.Abs(points[47].x - points[46].x) * 1.3);       // default : 6       

                //eyeCenterR.x = Math.Abs(points[45].x + points[42].x) / 2;
                //eyeCenterR.y = Math.Abs(points[45].y + points[42].y) / 2 - Math.Abs(points[47].x - points[46].x) * 0.1;
            }

            OpenCVForUnity.Rect roi_eye = new OpenCVForUnity.Rect(eye_left, eye_top, eye_w, eye_h);
            Mat cropimgMat_eye = new OpenCVForUnity.Mat(imgMat, roi_eye);

            //Mat grayMat = new Mat();
            Imgproc.cvtColor(cropimgMat_eye, cropimgMat_eye, Imgproc.COLOR_RGB2GRAY);
            //Imgproc.Canny(cropimgMat_eye, cropimgMat_eye, 50, 100);  // 50, 200 / 30 100

            // FRST //////////////////////////////////////////////////////////////////////////////           
            Point pupilpoint = extract_pupilUsingFRST(cropimgMat_eye, radii_start, radii_end, 2, 0.25, 9.9, 2);
            pupilpoint.x += eye_left;
            pupilpoint.y += eye_top;
            // FRST //////////////////////////////////////////////////////////////////////////////           

            //Imgproc.circle(imgMat, pupilpoint, 2, new Scalar(255, 0, 0, 255), 1);               // draw pupil
            //OpenCVForUnityUtils.draw_gazeLine(imgMat, eyeCenterR, pupilpoint, 18, 200);      // draw gaze Line

            return pupilpoint;
        }
        public static Point extract_pupilUsingFRST(Mat eyeMat,
                                                  int radii_start, int radii_end,
                                                  double alpha,
                                                  double stdFactor,
                                                  double k,
                                                  int mode
                                                  )
        {
            int w, h, dim, x, y;
            Point rpoint = new Point(0, 0);

            w = eyeMat.width();
            h = eyeMat.height();
            dim = w * h;

            float[] gx = new float[dim];
            float[] gy = new float[dim];
            float[] S = new float[dim];
            float[] S_n = new float[dim];
            float[] O_n = new float[dim];
            float[] M_n = new float[dim];

            // set the gradient map
            // gx, gy mat에 값이 잘 들어가는지 체크!!!!
            ImageProcess.gradx(eyeMat, gx, h, w);
            ImageProcess.grady(eyeMat, gy, h, w);

            // set dark/bright mode
            bool dark = false;
            bool bright = false;

            if (mode == FRST_MODE_BRIGHT) bright = true;
            else if (mode == FRST_MODE_DARK) dark = true;
            else if (mode == FRST_MODE_BOTH)
            {
                bright = true;
                dark = true;
            }
            else
            {
                Debug.Log("Invalid FRST mode!");
                return rpoint;
            }


            for (int r = radii_start; r <= radii_end; r++)
            {
                // set the On, Mn
                for (y = 0; y < h; y++)
                {
                    for (x = 0; x < w; x++)
                    {
                        S_n[y * w + x] = O_n[y * w + x] = M_n[y * w + x];

                        double gx_p = gx[y * w + x];
                        double gy_p = gy[y * w + x];
                        float gnorm = (float)Math.Sqrt(gx_p * gx_p + gy_p * gy_p);

                        if (gnorm > 0)
                        {
                            int tmp_x = (int)((gx_p / gnorm) * r + 0.5);
                            int tmp_y = (int)((gy_p / gnorm) * r + 0.5);

                            if (bright)
                            {
                                int ppve_x = x + tmp_x;
                                int ppve_y = y + tmp_y;
                                if (ppve_x >= w || ppve_y >= h || ppve_x < 0 || ppve_y < 0)
                                    continue;

                                O_n[ppve_y * w + ppve_x]++;
                                M_n[ppve_y * w + ppve_x] += gnorm;
                            }

                            if (dark)
                            {
                                int pnve_x = x - tmp_x;
                                int pnve_y = y - tmp_y;

                                if (pnve_x >= w || pnve_y >= h || pnve_x < 0 || pnve_y < 0)
                                    continue;

                                O_n[pnve_y * w + pnve_x]--;
                                M_n[pnve_y * w + pnve_x] -= gnorm;
                            }

                        } // end of if

                    } // end of x
                } // end of y

                if (k == 0.0)
                {
                    double maxO, maxM, valueO, valueM;
                    maxO = maxM = 0.0;
                    for (y = 0; y < h; y++)
                    {
                        for (x = 0; x < w; x++)
                        {
                            O_n[y * w + x] = Math.Abs(O_n[y * w + x]);
                            M_n[y * w + x] = Math.Abs(M_n[y * w + x]);

                            if (O_n[y * w + x] > maxO)
                                maxO = O_n[y * w + x];
                            if (M_n[y * w + x] > maxM)
                                maxM = M_n[y * w + x];
                        }
                    }

                    valueO = valueM = 0.0;
                    for (y = 0; y < h; y++)
                    {
                        for (x = 0; x < w; x++)
                        {

                            if (O_n[y * w + x] != 0) valueO = O_n[y * w + x] / maxO;
                            if (M_n[y * w + x] != 0) valueM = M_n[y * w + x] / maxM;
                            S_n[y * w + x] = (float)Math.Pow(valueO, alpha) * (float)valueM;        // calculate the F and S

                        }
                    }

                }
                else
                {
                    double valueO, valueM;
                    for (y = 0; y < h; y++)
                    {
                        for (x = 0; x < w; x++)
                        {
                            valueO = Math.Abs(O_n[y * w + x]);
                            valueM = Math.Abs(M_n[y * w + x]);

                            if (valueO > k)
                                valueO = k;

                            S_n[y * w + x] = (float)((valueM / k) * Math.Pow(valueO / k, alpha));  // calculate the Fn and Sn	

                        }
                    }
                }

                // smoothing	
                //double kSize = (r / 2.0) + 0.9;  // ceil
                double sigma = 0.25 * r;
                Mat smoothimg = new Mat(h, w, CvType.CV_32FC1);
                //if (kSize % 2 == 0)
                //    kSize++;
                smoothimg.put(0, 0, S_n);
                Imgproc.GaussianBlur(smoothimg, smoothimg, new Size(r, r), sigma);

                // calculate the sum S
                for (y = 0; y < h; y++)
                {
                    for (x = 0; x < w; x++)
                    {
                        if (r == radii_start)
                            S[y * w + x] = (float)smoothimg.get(y, x)[0]; //S_n[y * w + x];
                        else
                            S[y * w + x] += (float)smoothimg.get(y, x)[0]; //S_n[y * w + x];
                    }
                }

            } // end of for(r)

            // calculate the S & find max point in S
            int r_size = radii_end - radii_start + 1;
            float max_value = 0.0f;

            for (y = 0; y < h; y++)
            {
                for (x = 0; x < w; x++)
                {
                    S[y * w + x] /= r_size;

                    if (max_value < S[y * w + x])
                    {
                        max_value = S[y * w + x];
                        rpoint.x = x;
                        rpoint.y = y;
                    }
                }
            }

            return rpoint;
        }

        public static void draw_gazeLine(Mat imgMat, Point eyeCenterPoint, Point pupilPoint, double distError, double eyeball_z, double display_z)
        {
            Point drawPoint = new OpenCVForUnity.Point(eyeCenterPoint.x, eyeCenterPoint.y);            // eyeCenterPoint(원점)기준 좌표

            double eyeball_r;           // eyeball의 반지름
            //double eyeball_z = 18.0;    // eyeball의 중심에서 pupil까지의 거리
            //double display_z = 60.0;        // display를 위한 eyeball 중심에서 출력 z까지의 거리
            double theta = 0.0;         // θ radius (xy평면에서의 x축으로부터 pupil point의 각도)
            double phi = 0.0;           // Φ radius (zy평면에서의 z축으로부터 pupil point의 각도) = arccos(z/r) 

            double dist = getDistance(pupilPoint, eyeCenterPoint);
            Point tmpPupilPoint = new Point(0, 0);
            if (dist < distError)
            {
                tmpPupilPoint.x = pupilPoint.x;
                tmpPupilPoint.y = pupilPoint.y;
                //tmpPupilPoint.x = pupilPoint.x - eyeCenterPoint.x - 1;
                //tmpPupilPoint.y = pupilPoint.y - eyeCenterPoint.y + 1;
                pupilPoint.x = eyeCenterPoint.x + 1;
                pupilPoint.y = eyeCenterPoint.y - 1;
            }

            pupilPoint.x -= eyeCenterPoint.x;
            pupilPoint.y -= eyeCenterPoint.y;

            eyeball_r = Math.Sqrt(pupilPoint.x * pupilPoint.x + pupilPoint.y * pupilPoint.y + eyeball_z * eyeball_z);
            phi = Math.Acos(eyeball_z / eyeball_r);

            // xy평면의 θ구하기(x축으로부터 pupil point의 각도)
            theta = Math.Atan2(pupilPoint.y, pupilPoint.x);

            // r=40pixel일 경우 pupuilpoint'구하기
            drawPoint.x = display_z * Math.Cos(theta) * Math.Sin(phi);
            drawPoint.y = display_z * Math.Sin(theta) * Math.Sin(phi);

            // pupil, gaze line 그리기	
            pupilPoint.x += eyeCenterPoint.x;
            pupilPoint.y += eyeCenterPoint.y;
            drawPoint.x += eyeCenterPoint.x;
            drawPoint.y += eyeCenterPoint.y;

            if (dist < distError)
            {
                drawPoint.x += tmpPupilPoint.x - pupilPoint.x;
                drawPoint.y += tmpPupilPoint.y - pupilPoint.y;
                pupilPoint = tmpPupilPoint;
            }


            Imgproc.line(imgMat, drawPoint, pupilPoint, new Scalar(255, 228, 0, 255), 2);
            //Imgproc.circle(imgMat, pupilPoint, 1, new Scalar(0, 255, 255, 255), 1);

            /*string strMsg;
            strMsg = "pupile(" + pupilPoint.x.ToString() + ", " + pupilPoint.y.ToString() + ")";
            Debug.Log(strMsg);
            strMsg = "cEye(" + eyeCenterPoint.x.ToString() + ", " + eyeCenterPoint.y.ToString() + ")";
            Debug.Log(strMsg);
            Debug.Log("Dist : " + dist.ToString());   */
        }

    }  // end of class  

} // end of namespace
