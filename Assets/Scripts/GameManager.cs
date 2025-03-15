using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.PostProcessing;



public class GameManager : MonoBehaviour
{

    public enum GameState
    {
        MainMenu,
        InGame,
        PauseMenu,
        Instructions,
        DeathScreen,
        EndRound,
        FinalScreen
    }

    [SerializeField] private Maze maze;
    [SerializeField] private ControllerInput player;
    [SerializeField] private Rope rope;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject deathScreenPanel;
    [SerializeField] private GameObject endRoundPanel;
    [SerializeField] private GameObject finalScreenPanel;
    [SerializeField] private GameObject inGameOverlayPanel;
    [SerializeField] private GameObject instructionsPanel;
    [SerializeField] private GameObject inGameMapPanel;
    [SerializeField] private TextMeshProUGUI roundIndicatorText;
    [SerializeField] private PostProcessVolume postProcessVolume;
    [SerializeField] private PostProcessProfile postProcessLevelOne;
    [SerializeField] private PostProcessProfile postProcessLevelTwo;


    private GameState currentState;

    public GameState State => currentState;

    public void PausePlayback()
    {
        Time.timeScale = 0;
    }

    public void ResumePlayback()
    {
        Time.timeScale = 1;
    }

    private void HideAll()
    {
        mainMenuPanel.SetActive(false);
        pauseMenuPanel.SetActive(false);
        deathScreenPanel.SetActive(false);
        endRoundPanel.SetActive(false);
        finalScreenPanel.SetActive(false);
        inGameOverlayPanel.SetActive(false);
        instructionsPanel.SetActive(false);
    }

    public void Start()
    {
        HideAll();
        mainMenuPanel.SetActive(true);
        currentState = GameState.MainMenu;
        ResumePlayback();
    }

    public void ShowDeathScreen()
    {
        HideAll();
        deathScreenPanel.SetActive(true);
        currentState = GameState.DeathScreen;
        PausePlayback();
    }

    public void ShowEndRoundScreen()
    {
        HideAll();
        endRoundPanel.SetActive(true);
        currentState = GameState.EndRound;
        PausePlayback();
    }

    public void ShowFinalScreen()
    {
        HideAll();
        finalScreenPanel.SetActive(true);
        currentState = GameState.FinalScreen;
        PausePlayback();
    }

    public void OnExitToDesktopPressed()
    {
        Application.Quit();
    }
    
    public void OnMapButtonPressed()
    {
        if (currentState != GameState.InGame)
            return;

        inGameMapPanel.SetActive(!inGameMapPanel.activeSelf);
    }

    public void OnRouteButtonPressed()
    {
        if (currentState != GameState.InGame)
            return;

        maze.route.gameObject.SetActive(!maze.route.gameObject.activeSelf);
    }

    public void OnNewGamePressed()
    {
        HideAll();
        inGameOverlayPanel.SetActive(true);
        
        maze.FirstRound();
        maze.Restart();
        LevelOneEffects();
        SetRoundIndicatorText(maze.level, maze.round);

        currentState = GameState.InGame;
        ResumePlayback();
    }

    [ContextMenu("LevelOneEffects")]
    public void LevelOneEffects()
    {
        postProcessVolume.profile = postProcessLevelOne;
        rope.ActivateRealisticRope();
    }


    [ContextMenu("LevelTwoEffects")]
    public void LevelTwoEffects()
    {
        postProcessVolume.profile = postProcessLevelTwo;
        rope.ActivateUnrealisticRope();
    }

    public void OnInstructionsPressed()
    {
        HideAll();
        instructionsPanel.SetActive(true);
        currentState = GameState.Instructions;
        PausePlayback();
    }
    

    public void OnPausePressed()
    {
        HideAll();
        pauseMenuPanel.SetActive(true);
        currentState = GameState.PauseMenu;
        PausePlayback();
    }

    public void OnResumePressed()
    {
        HideAll();
        inGameOverlayPanel.SetActive(true);
        currentState = GameState.InGame;
        ResumePlayback();
    }

    public void OnExitToMainMenuPressed()
    {
        HideAll();
        mainMenuPanel.SetActive(true);
        currentState = GameState.MainMenu;
        PausePlayback();
    }

    public void OnNextRoundPressed()
    {
        HideAll();
        inGameOverlayPanel.SetActive(true);
        maze.NextRound();
        SetRoundIndicatorText(maze.level, maze.round);
        maze.Restart();
        if (maze.level == 2) 
            LevelTwoEffects();
        currentState = GameState.InGame;
        ResumePlayback();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            switch (currentState)
            {
                case GameState.MainMenu:
                    OnExitToDesktopPressed();
                    break;

                case GameState.InGame:
                    OnPausePressed();
                    break;

                case GameState.PauseMenu:   
                    OnResumePressed();
                    break;

                case GameState.DeathScreen:
                    OnExitToMainMenuPressed();
                    break;

                case GameState.EndRound:
                    OnNextRoundPressed();
                    break;

                case GameState.FinalScreen:
                    OnExitToMainMenuPressed();
                    break;

                case GameState.Instructions:
                    OnExitToMainMenuPressed();
                    break;
            }
        }


        if (Input.GetKeyDown(KeyCode.Tab))
        {
            OnMapButtonPressed();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnRouteButtonPressed();
        }
    }

    
    public void SetRoundIndicatorText(int level, int round)
    {
        int points = (level - 1) * 3 + (round - 1);

        roundIndicatorText.text = $"Level {level} - Round {round} ({points} points)";
    }
}
