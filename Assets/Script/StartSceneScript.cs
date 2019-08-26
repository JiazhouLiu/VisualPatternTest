﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using UnityEngine.UI;
using VRTK;

public class StartSceneScript : MonoBehaviour
{
    public int ExperimentID;
    public int TrialNumber;

    public static int controllerHand = -1;
    public static int ExperimentSequence;
    public static int ParticipantID;
    public static string CurrentDateTime;
    public static int PublicTrialNumber;
    public static float lastTimePast;

    private VRTK_ControllerEvents leftCE;
    private VRTK_ControllerEvents rightCE;

    // Start is called before the first frame update
    void Start()
    {
        if (leftCE == null) {
            leftCE = GameObject.Find("LeftControllerAlias").GetComponent<VRTK_ControllerEvents>();
        }

        if (rightCE == null) {
            rightCE = GameObject.Find("RightControllerAlias").GetComponent<VRTK_ControllerEvents>();
        }

        PublicTrialNumber = TrialNumber;

        if (ExperimentID > 0)
        {
            ParticipantID = ExperimentID;

            // TBC
            switch (ExperimentID % 4)
            {
                case 1:
                    ExperimentSequence = 1;
                    break;
                case 2:
                    ExperimentSequence = 2;
                    break;
                case 3:
                    ExperimentSequence = 3;
                    break;
                case 0:
                    ExperimentSequence = 4;
                    break;
                default:
                    break;
            }
        }
        else
        { // testing stream
            ExperimentSequence = 1;
        }

        if (TrialNumber == 0)
        {
            CurrentDateTime = GetDateTimeString();

            // Raw data log
            string writerFilePath = "Assets/ExperimentData/ExperimentLog/Participant " + ParticipantID + "/Participant_" + ParticipantID + "_RawData.csv";
            StreamWriter writer = new StreamWriter(writerFilePath, false);
            string logFileHeader = "TimeSinceStart,TrialNo,TrialID,ParticipantID,ExperimentSequence,Layout,MemoryType,TrialState,CameraPosition.x," +
                "CameraPosition.y,CameraPosition.z,CameraEulerAngles.x,CameraEulerAngles.y,CameraEulerAngles.z,LeftPadPressed,RightPadPressed,LeftTriggerPressed,RightTriggerPressed";
            writer.WriteLine(logFileHeader);
            writer.Close();

            // Answers data log
            string writerAnswerFilePath = "Assets/ExperimentData/ExperimentLog/Participant " + ParticipantID + "/Participant_" + ParticipantID + "_Answers.csv";
            writer = new StreamWriter(writerAnswerFilePath, false);
            writer.WriteLine("ParticipantID,TrialNo,TrialID,Layout,MemoryType,DifficultyLevel,AccurateAnswer,AllSeenTime,AllSelectTime");
            writer.Close();
        }
        else
        {
            string lastFileName = "";

            string folderPath = "Assets/ExperimentData/ExperimentLog/Participant " + ParticipantID + "/";
            DirectoryInfo info = new DirectoryInfo(folderPath);
            FileInfo[] fileInfo = info.GetFiles();
            foreach (FileInfo file in fileInfo)
            {
                if (file.Name.Contains("Participant_" + ParticipantID + "_RawData.csv") && !file.Name.Contains("meta"))
                {
                    lastFileName = file.Name;
                }
            }
            if (lastFileName == "")
            {
                Debug.LogError("No previous file found!");
            }
            else
            {
                string writerFilePath = "Assets/ExperimentData/ExperimentLog/Participant " + ParticipantID + "/" + lastFileName;
                //Debug.Log(File.ReadAllLines(writerFilePath).Length);
                string lastLine = File.ReadAllLines(writerFilePath)[File.ReadAllLines(writerFilePath).Length - 1];
                float lastTime = float.Parse(lastLine.Split(',')[0]);

                lastTimePast = lastTime;

                //switch (ExperimentSequence)
                //{
                    
                //}
            }
        }
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == "StartScene")
        {
            if (leftCE != null && rightCE != null)
            {
                if (controllerHand == -1)
                {
                    if (leftCE.touchpadPressed)
                    {
                        controllerHand = 0;
                    }

                    if (rightCE.touchpadPressed)
                    {
                        controllerHand = 1;
                    }
                }
                else
                {
                    SceneManager.LoadScene("Experiment", LoadSceneMode.Single);
                }
            }
        }
    }

    string GetDateTimeString()
    {
        return DateTime.Now.Month.ToString("D2") + DateTime.Now.Day.ToString("D2") + "-" + DateTime.Now.Hour.ToString("D2") + DateTime.Now.Minute.ToString("D2") + DateTime.Now.Second.ToString("D2");
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
