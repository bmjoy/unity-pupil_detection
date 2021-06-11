using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;
using FERdll;
using PupilDetectionDLL;

public class EmotionDetection {

    public EmotionDetection()
    {       
    }        

    public void DrawEmotion(Mat imgMat, UnityEngine.Rect rect, int loaded_class_cnt, int resultEmotion)
    {
        string strEmotion = null;

        if( loaded_class_cnt == 7)
        {
            switch (resultEmotion)
            {
                case 0:
                    strEmotion = "neutral";
                    break;
                case 1:
                    strEmotion = "anger";
                    break;
                case 2:
                    strEmotion = "disgust";
                    break;
                case 3:
                    strEmotion = "fear";
                    break;
                case 4:
                    strEmotion = "happy";
                    break;
                case 5:
                    strEmotion = "sadness";
                    break;
                case 6:
                    strEmotion = "surprise";
                    break;
                default:
                    strEmotion = "???";
                    break;
            }
        }else if(loaded_class_cnt == 3)
        {
            switch (resultEmotion)
            {
                case 0:
                    strEmotion = "neutral";
                    break;
                case 1:
                    strEmotion = "negative";
                    break;
                case 2:
                    strEmotion = "positive";
                    break;
                default:
                    strEmotion = "???";
                    break;
            }
        }
        
        Imgproc.putText(imgMat, strEmotion, new Point(rect.xMin, rect.yMax + (rect.width * 0.2)), Core.FONT_HERSHEY_COMPLEX, 0.8, new Scalar(0, 255, 0, 255), 1);

    }

    /// <summary>
    /// Draws positions and radius of pupils
    /// </summary>
    public void DrawPupils(Mat imgMat, PPoint pupil_pl, PPoint pupil_pr, int radius_l, int radius_r, Scalar pupil_color, Scalar radius_color)
    {
        if (pupil_pl.x >= 0)
        {
            Imgproc.circle(imgMat, new Point(pupil_pl.x, pupil_pl.y), 2, pupil_color, 1);                // center point
            Imgproc.circle(imgMat, new Point(pupil_pl.x, pupil_pl.y), radius_l + 1, radius_color, 1);     // radius
        }

        if (pupil_pr.x >= 0)
        {
            Imgproc.circle(imgMat, new Point(pupil_pr.x, pupil_pr.y), 2, pupil_color, 1);                // center point
            Imgproc.circle(imgMat, new Point(pupil_pr.x, pupil_pr.y), radius_r + 1, radius_color, 1);     // radius
        }
    }

    /// <summary>
    /// Draws the face landmark.This method supports 68 landmark points.
    /// </summary>   
    public void DrawFaceLandmark(Mat imgMat, List<Vector2> points, Scalar color, int thickness)
    {
        if (points.Count == 68)
        {
            for (int i = 0; i < points.Count; i++)
            {
                Imgproc.circle(imgMat, new Point(points[i].x, points[i].y), 1, color, thickness);
                //Imgproc.putText(imgMat, i.ToString(), new Point(points[i].x, points[i].y), 1, 0.5, new Scalar(0, 0, 0, 255),1); // added Jueun 2019.12.23
            }
            
        }
    }

    /// <summary>
    /// Draws the face rect.
    /// </summary>
    public void DrawFaceRect(Mat imgMat, UnityEngine.Rect rect, Scalar color, int thickness)
    {
        Imgproc.rectangle(imgMat, new Point(rect.xMin, rect.yMin), new Point(rect.xMax, rect.yMax), color, thickness);
    }

    /// <summary>
    /// Draws text.
    /// </summary>
    public void DrawText(Mat imgMat, string msg, Point pt, int fontFace, double fontScale, Scalar color, int thickness)
    {
        Imgproc.putText(imgMat, msg, pt, fontFace, fontScale, color, thickness, Core.LINE_AA, false);
    }
    

}
