using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenCVForUnity;

namespace Assets.DlibFaceLandmarkDetectorWithOpenCVExample
{
    class ImageProcess
    {

        public static int OCSLBP_BIN_SIZE = 8;
        public static int OCSLBP_THRESHOLD = 3; // 6->3
        public static int BLOCK_COUNT = 3;      // 3x3
        

        public static void getOCSLBPfeature(Mat imgMat, double[] featureVector, int left, int top, int right, int bottom)
        {
            int i, j, m, n;

            int bn = BLOCK_COUNT;
            int sub_block_count = bn * bn;        // 3X3
            int vector_dim = OCSLBP_BIN_SIZE * sub_block_count;
            //double[] featureVector = new double[vector_dim];

            int height = bottom - top;
            int width = right - left;

            int t = OCSLBP_THRESHOLD; // 4  2013-12-12 JMR 100->6
            int startY = 0;
            int endY = 0;
            int startX = 0;
            int endX = 0;

            // Building histogram (region)
            int marginY = height % bn;
            int marginX = width % bn;

            double[,] histogram = new double[sub_block_count, OCSLBP_BIN_SIZE];
            System.Array.Clear(histogram, 0, histogram.Length);

            // convert image from color to gray
            Mat grayimgMat = new Mat();
            if ( imgMat.channels() != 1 )
            {               
                Imgproc.cvtColor(imgMat, grayimgMat, Imgproc.COLOR_BGR2GRAY);
            }else
            {
                imgMat.copyTo(grayimgMat);
            }           

            for (j = 0; j < bn; j++)
            {
                for (i = 0; i < bn; i++)
                {
                    startY = j * (height / bn) + top;
                    endY = (j + 1) * (height / bn) + top;
                    startX = i * (width / bn) + left;
                    endX = (i + 1) * (width / bn) + left;

                    for (m = startY; m < endY; m++)
                    {
                        for (n = startX; n < endX; n++)
                        {
                            if (m == 0 || n == 0)
                                continue;

                            if (m == imgMat.height() - marginY - 1 || n == imgMat.width() - marginX - 1)
                                continue;

                            double diff1 = grayimgMat.get(m, n + 1)[0] - grayimgMat.get(m, n - 1)[0];
                            double diff2 = grayimgMat.get(m, n - 1)[0] - grayimgMat.get(m, n + 1)[0];
                            if (diff1 >= OCSLBP_THRESHOLD)                        // 0
                            {
                                //histogram[j * bn + i][0] += abs(ptr[m][n + 1] - ptr[m][n - 1]);
                                histogram[j * bn + i, 0] += Math.Abs(diff1);
                            }
                            else if (diff2 >= OCSLBP_THRESHOLD)
                            {
                                histogram[j * bn + i, 4] += Math.Abs(diff2);
                            }

                            diff1 = grayimgMat.get(m + 1, n + 1)[0] - grayimgMat.get(m - 1, n - 1)[0];
                            diff2 = grayimgMat.get(m - 1, n - 1)[0] - grayimgMat.get(m + 1, n + 1)[0];
                            if (diff1 >= OCSLBP_THRESHOLD)                                  // 1
                            {
                                histogram[j * bn + i, 1] += Math.Abs(diff1);
                            }
                            else if (diff2 >= OCSLBP_THRESHOLD)                            // 5
                            {
                                histogram[j * bn + i, 5] += Math.Abs(diff2);
                            }

                            diff1 = grayimgMat.get(m + 1, n)[0] - grayimgMat.get(m - 1, n)[0];
                            diff2 = grayimgMat.get(m - 1, n)[0] - grayimgMat.get(m + 1, n)[0];
                            if (diff1 >= OCSLBP_THRESHOLD)                                      // 2
                            {
                                histogram[j * bn + i, 2] += Math.Abs(diff1);
                            }
                            else if (diff2 >= OCSLBP_THRESHOLD)                                 // 6
                            {
                                histogram[j * bn + i, 6] += Math.Abs(diff2);
                            }

                            diff1 = grayimgMat.get(m + 1, n - 1)[0] - grayimgMat.get(m - 1, n + 1)[0];
                            diff2 = grayimgMat.get(m - 1, n + 1)[0] - grayimgMat.get(m + 1, n - 1)[0];
                            if (diff1 >= OCSLBP_THRESHOLD)                                  // 3
                            {
                                histogram[j * bn + i, 3] += Math.Abs(diff1);
                            }
                            else if (diff2 >= OCSLBP_THRESHOLD)                             // 7
                            {
                                histogram[j * bn + i, 7] += Math.Abs(diff2);
                            }

                        } // end of for n

                    } // end of for m

                } // end of for i

            } // end of for j

            for (i = 0; i < sub_block_count; i++)
            {
                for (j = 0; j < OCSLBP_BIN_SIZE; j++)
                {
                    featureVector[i * OCSLBP_BIN_SIZE + j] = histogram[i, j];
                }
            }

            minMaxNormalize(featureVector, vector_dim);

        } // end of method 


        public static void minMaxNormalize(double[] histogram, int histBins)
        {
            int i;            

            double Nmin = 0.0;
            double Nmax = 1.0;

            double minValue = 65536;
            double maxValue = -1;

            for (i = 0; i < histBins; i++)
            {
                if (minValue > histogram[i])
                {
                    minValue = histogram[i];
                }
                if (maxValue < histogram[i])
                {
                    maxValue = histogram[i];
                }
            }

            for (i = 0; i < histBins; i++)
            {
                if (maxValue - minValue != 0)
                {
                    histogram[i] = (histogram[i] - minValue) * ((Nmax - Nmin) / (maxValue - minValue)) + Nmin;
                }
                else
                {
                    histogram[i] = 0.0;
                }
            }

        } // end of method

        /**
            @brief      Calculate vertical gradient for the input image
	        @param      Input img : img Input 8-bit image 
            @param      Output img : img Output 8-bit image
	        @param      height : img height
            @param      width : img width
         */
        public static void gradx(Mat Inputimg, float[] OutputImg, int height, int width)
        {
            float gradient = 0.0f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    if (Inputimg.get(y,x)[0] > 200.0)
                        gradient = 0;
                    else
                    {
                        gradient = ((float)Inputimg.get(y, x+1)[0] - (float)Inputimg.get(y, x-1)[0]) / 2.0f;
                        
                        //gradient = Math.Abs(gradient);
                        // if (Math.Abs(gradient) > 30.0)
                        //    gradient = 0.0f;
                    }

                    if (x == 1)
                    {
                        OutputImg[y*width + x - 1] = gradient;
                        OutputImg[y*width + x] = gradient; 
                    }
                    else if (x == width - 2)
                    {
                        OutputImg[y*width + x + 1] = gradient;
                        OutputImg[y*width + x] = gradient;
                    }
                    else
                    {
                        OutputImg[y*width + x] = gradient;
                    }
                }
            }

        } // end of method

        /**
            @brief      Calculate horizontal gradient for the input image
	        @param      Input img : img Input 8-bit image 
            @param      Output img : img Output 8-bit image
	        @param      height : img height
            @param      width : img width
         */
        public static void grady(Mat Inputimg, float[] OutputImg, int height, int width)
        {
            float gradient = 0.0f;

            for (int y = 1; y < height-1; y++)
            {
                for (int x = 0; x < width ; x++)
                {
                    if (Inputimg.get(y, x)[0] > 200.0)
                        gradient = 0.0f;
                    else
                    {
                        gradient = ((float)Inputimg.get(y+1, x)[0]  - (float)Inputimg.get(y-1, x)[0]) / 2.0f;       // /2.0;
                        
                        //gradient = Math.Abs(gradient);
                    }

                    if (y == 1)
                    {
                        OutputImg[(y-1)*width + x ] =  gradient;
                        OutputImg[y*width + x] = gradient;                        
                    }
                    else if (y == height - 2)
                    {
                        OutputImg[(y+1)*width + x] = gradient;  
                        OutputImg[y*width + x] = gradient;
                    }
                    else
                    {
                        OutputImg[y*width + x] = gradient;  
                    }
                }
            }

        } // end of method




    }
}
