using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using TMPro;
using UnityEngine.UI;

public enum GameState
{
    Prepare,
    ShowPattern, // 10s
    SelectCards,
    Result
}

public enum MemoryType
{
    VS,
    DKVS,
    IKVS,
}

public class ExperimentManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject CardPrefab;
    public Sprite ShapePrefab1;
    public Text DashBoardText;
    public Text SeenCardText;
    public Text SelectCardText;
    public Text MemoryTypeText;
    public TextMeshProUGUI LeftControllerText;
    public TextMeshProUGUI RightControllerText;

    [Header("Predefined Variables")]
    public float hDelta;
    public float vDelta;
    public float cardSize;
    public float memoryTime;
    public int numberOfRows;
    public int numberOfColumns;

    [Header("Variables")]
    public GameData gameData;
    public Layout layout;
    public MemoryType memoryType;

    /// <summary>
    /// local variables
    /// </summary>

    // do not change
    private List<Text> Instructions;
    public int controllerHand = 1; // 0: left; 1: right
    private int difficultyLevel;
    private VRTK_InteractUse mainHandIU;
    private VRTK_ControllerEvents mainHandCE;

    // incremental with process
    private GameState gameState;
    private int trialNo = 0;

    // refresh every trail
    private List<GameObject> cards;
    private List<GameObject> selectedCards;
    private bool correctTrial; // true if user answer all correct cards

    // refresh in one process
    private bool showingPattern = false; // show pattern stage
    private bool startCount = false; // show pattern stage
    private bool allSeen = false;
    private float scanTime = 0;
    private bool allSelected = false;
    private float selectTime = 0;

    // check on update for interaction
    private bool localTouchpadPressed = false;
    private bool localMenuPressed = false;
    private bool instruction = true;

    // log use
    private string trialID;
    private int experimentSequence;
    private float currentAllSeenTime;
    private float currentAllSelectTime;


    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(StartSceneScript.ParticipantID + " " + StartSceneScript.PublicTrialNumber);
        // initialise variables
        cards = new List<GameObject>();
        selectedCards = new List<GameObject>();
        Instructions = new List<Text>
        {
            SeenCardText,
            SelectCardText,
        };

        if (StartSceneScript.controllerHand == 0 || StartSceneScript.controllerHand == 1)
            controllerHand = StartSceneScript.controllerHand;
        else
            controllerHand = 1;

        trialNo++;

        // setup experiment
        PrepareExperiment();
    }

    // Update is called once per frame
    void Update()
    {
        // change layout shortcut
        if (Input.GetKeyDown("c"))
            Changelayout();

        if (Input.GetKeyDown("m"))
            ChangeMemoryTypes();

        if (gameState == GameState.SelectCards) {
            GameInteraction();
            PrintTextToScreen(DashBoardText, "Total number of white cards: <color=green>" + difficultyLevel + "</color>\nYou have selected: <color=green>" + selectedCards.Count + "</color>\n\nPlease press <color=green>Finish</color> button when you finish.");
        }
            

        if (gameState == GameState.ShowPattern)
            TimerAndCheckScan();

        CheckStateChange();
    }

    // check button pressed for state change
    private void CheckStateChange() {

        // assign left or right controllers controller events
        if (mainHandCE == null)
        {
            if (controllerHand == 0 && GameObject.Find("LeftControllerAlias") != null)
                mainHandCE = GameObject.Find("LeftControllerAlias").GetComponent<VRTK_ControllerEvents>();
            else if (controllerHand == 1 && GameObject.Find("RightControllerAlias") != null)
                mainHandCE = GameObject.Find("RightControllerAlias").GetComponent<VRTK_ControllerEvents>();
        }
        else {
            if (mainHandCE.touchpadPressed)
            {
                localTouchpadPressed = true;
            }
            else {
                if (localTouchpadPressed) {
                    localTouchpadPressed = false;
                    switch (gameState)
                    {
                        case GameState.Prepare:
                            ShowPattern();
                            break;
                        case GameState.ShowPattern:   
                            break;
                        case GameState.SelectCards:
                            LeftControllerText.text = "Ready";
                            RightControllerText.text = "Ready";
                            FinishAnswering();
                            PrintTextToScreen(DashBoardText, "Great!\nPlease press <color=green>Ready</color> button to set up a new game.");
                            //if (correctTrial)
                            //    PrintTextToScreen(DashBoardText, "<color=green>Correct!</color>\nPlease press <color=green>Ready</color> button to set up a new game.");
                            //else
                            //    PrintTextToScreen(DashBoardText, "<color=red>Wrong!</color>\nPlease press <color=green>Ready</color> button to set up a new game.");
                            break;
                        case GameState.Result:
                            LeftControllerText.text = "Start";
                            RightControllerText.text = "Start";
                            PrepareExperiment();
                            break;
                        default:
                            break;
                    }
                }
            }

            // toggle instructions
            if (mainHandCE.buttonTwoPressed)
            {
                localMenuPressed = true;
            }
            else {
                if (localMenuPressed) {
                    if (instruction)
                    {
                        instruction = false;
                        foreach (Text text in Instructions)
                        {
                            text.gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        instruction = true;
                        foreach (Text text in Instructions)
                        {
                            text.gameObject.SetActive(true);
                        }
                    }
                }
                localMenuPressed = false;
            }
        }
    }

    // Prepare stage (after clicking ready button)
    private void PrepareExperiment() {
        gameState = GameState.Prepare;

        PrintTextToScreen(DashBoardText, "Please press <color=green>Start</color> button to start the memory game.");
        //if (correctTrial) {
        //    trialNo++;
        //    workingMemoryLoad = 2 * trialNo;

        //    correctTrial = false;
        //}

        PrintTextToScreen(SeenCardText, "");
        PrintTextToScreen(SelectCardText, "");
        PrintTextToScreen(MemoryTypeText, "");

        if (memoryType == MemoryType.DKVS)
        {
            if (controllerHand == 0)
            {
                GameObject.Find("LeftControllerAlias").GetComponent<VRTK_StraightPointerRenderer>().maximumLength = 0.1f;
                GameObject.Find("LeftControllerAlias").GetComponent<VRTK_StraightPointerRenderer>().cursorScaleMultiplier = 10;
            }
            else {
                GameObject.Find("RightControllerAlias").GetComponent<VRTK_StraightPointerRenderer>().maximumLength = 0.1f;
                GameObject.Find("RightControllerAlias").GetComponent<VRTK_StraightPointerRenderer>().cursorScaleMultiplier = 10;
            }
        }
        else {
            if (controllerHand == 0)
            {
                GameObject.Find("LeftControllerAlias").GetComponent<VRTK_StraightPointerRenderer>().maximumLength = 12;
                GameObject.Find("LeftControllerAlias").GetComponent<VRTK_StraightPointerRenderer>().cursorScaleMultiplier = 25;
            }
            else
            {
                GameObject.Find("RightControllerAlias").GetComponent<VRTK_StraightPointerRenderer>().maximumLength = 12;
                GameObject.Find("RightControllerAlias").GetComponent<VRTK_StraightPointerRenderer>().cursorScaleMultiplier = 25;
            }
        }

        if (cards != null)
        {
            foreach (GameObject go in cards)
                Destroy(go);
            cards.Clear();

            foreach (GameObject go in selectedCards)
                Destroy(go);
            selectedCards.Clear();

            allSeen = false;
            allSelected = false;

            cards = GenerateCards();
            ShuffleCards();
        }
        else {
            // generate and shuffle cards
            cards = GenerateCards();
            ShuffleCards();
        }

        LeftControllerText.text = "Start";
        RightControllerText.text = "Start";
    }

    // Show pattern (after clicking Start button)
    private void ShowPattern() {
        gameState = GameState.ShowPattern;
        showingPattern = true;
        // start timer
        startCount = true;

        // Hide controller in DKVS
        if (memoryType == MemoryType.VS) {
            if (controllerHand == 0)
            {
                GameObject.Find("Controller (left)").transform.GetChild(0).gameObject.SetActive(false);
                GameObject.Find("LeftControllerAlias").GetComponent<VRTK_Pointer>().enabled = false;
                GameObject.Find("LeftControllerAlias").GetComponent<VRTK_StraightPointerRenderer>().enabled = false;
                LeftControllerText.text = "";
            }
            else {
                GameObject.Find("Controller (right)").transform.GetChild(0).gameObject.SetActive(false);
                GameObject.Find("RightControllerAlias").GetComponent<VRTK_Pointer>().enabled = false;
                GameObject.Find("RightControllerAlias").GetComponent<VRTK_StraightPointerRenderer>().enabled = false;
                RightControllerText.text = "";
            }
        }

        // flip to the front
        foreach (GameObject card in cards) {
            if (IsCardFilled(card))
                SetCardsColor(card.transform, Color.white);
            StartCoroutine(Rotate(card.transform, new Vector3(0, 180, 0), 0.5f));
        }
    }

    // Hide pattern 
    private void HidePattern() {
        showingPattern = false;

        // reset timer and other variables
        startCount = false;
        memoryTime = 10.0f;
        scanTime = 0f;
        selectTime = 0f;

        PrintTextToScreen(SeenCardText, "");
        PrintTextToScreen(SelectCardText, "");
        PrintTextToScreen(MemoryTypeText, "");
        LeftControllerText.text = "Finish";
        RightControllerText.text = "Finish";

        // show controller in DKVS
        if (memoryType == MemoryType.VS)
        {
            if (controllerHand == 0)
            {
                GameObject.Find("Controller (left)").transform.GetChild(0).gameObject.SetActive(true);
                GameObject.Find("LeftControllerAlias").GetComponent<VRTK_Pointer>().enabled = true;
                GameObject.Find("LeftControllerAlias").GetComponent<VRTK_StraightPointerRenderer>().enabled = true;
            }
            else
            {
                GameObject.Find("Controller (right)").transform.GetChild(0).gameObject.SetActive(true);
                GameObject.Find("RightControllerAlias").GetComponent<VRTK_Pointer>().enabled = true;
                GameObject.Find("RightControllerAlias").GetComponent<VRTK_StraightPointerRenderer>().enabled = true;
            }
        }

        // flip to the back
        foreach (GameObject card in cards)
        {
            if (IsCardFilled(card))
                SetCardsColor(card.transform, Color.black);
            StartCoroutine(Rotate(card.transform, new Vector3(0, 180, 0), 0.5f));
        }

        // move to next state
        gameState = GameState.SelectCards;
        // enable the interactable feature
        foreach (GameObject card in cards) {
            card.GetComponent<VRTK_InteractableObject>().enabled = true;
        }

    }

    // Check Result (after clicking Finish Button)
    private void FinishAnswering() {
        gameState = GameState.Result;
        correctTrial = CheckResult();

        // Write to Log
        // TO DO
    }


    ///////////////////////////////////////////////////////////////////////
    /// Game Logic
    ///////////////////////////////////////////////////////////////////////

    // check card interaction with pointer
    private void GameInteraction()
    {
        GameObject selectedCard = null;

        // assign left and right controllers interaction use
        if (mainHandIU == null) {
            if (controllerHand == 0 && GameObject.Find("LeftControllerAlias") != null)
                mainHandIU = GameObject.Find("LeftControllerAlias").GetComponent<VRTK_InteractUse>();
            else if (controllerHand == 1 && GameObject.Find("RightControllerAlias") != null)
                mainHandIU = GameObject.Find("RightControllerAlias").GetComponent<VRTK_InteractUse>();
        }

        if (mainHandIU.GetUsingObject() != null && !IsCardRotating(mainHandIU.GetUsingObject()))
        {
            selectedCard = mainHandIU.GetUsingObject();
            if (!IsCardFlipped(selectedCard)) // not flipped
            {
                selectedCards.Add(selectedCard);
                selectedCard.GetComponent<Card>().flipped = true;
                StartCoroutine(Rotate(selectedCard.transform, new Vector3(0, 180, 0), 0.5f));
                SetCardsColor(selectedCard.transform, Color.white);
            }
            //else
            //{ // flipped
            //    selectedCards.Remove(selectedCard);
            //    selectedCard.GetComponent<Card>().flipped = false;
            //    StartCoroutine(Rotate(selectedCard.transform, new Vector3(0, 180, 0), 1f));
            //    SetCardsColor(selectedCard.transform, Color.black);
            //}
        }
    }

    // check the result
    private bool CheckResult() {
        bool finalResult = true;
        int correctNum = 0;

        if (selectedCards.Count != difficultyLevel) {
            finalResult = false;
        }
        else {
            foreach (GameObject selectedCard in selectedCards)
            {
                if (!IsCardFilled(selectedCard))
                {
                    finalResult = false;
                }
                else {
                    correctNum++;
                }
            }
        }
        Debug.Log(correctNum + "/" + difficultyLevel);
        return finalResult;
    }

    // Change Layout
    private void Changelayout()
    {
        switch (layout)
        {
            case Layout.Flat:
                layout = Layout.SemiCircle;
                break;
            case Layout.SemiCircle:
                layout = Layout.FullCircle;
                break;
            case Layout.FullCircle:
                layout = Layout.Cube;
                break;
            case Layout.Cube:
                layout = Layout.Flat;
                break;
            default:
                break;
        }

        PrepareExperiment();
    }

    // Change Layout
    private void ChangeMemoryTypes()
    {
        switch (memoryType)
        {
            case MemoryType.VS:
                memoryType = MemoryType.DKVS;
                break;
            case MemoryType.DKVS:
                memoryType = MemoryType.IKVS;
                break;
            case MemoryType.IKVS:
                memoryType = MemoryType.VS;
                break;
            default:
                break;
        }

        PrepareExperiment();
    }

    // Generate Cards
    private List<GameObject> GenerateCards()
    {
        List<GameObject> cards = new List<GameObject>();
        int k = 0;

        if (layout != Layout.Cube)
        {
            for (int i = 0; i < numberOfRows; i++)
            {
                for (int j = 0; j < numberOfColumns; j++)
                {
                    // calculate index number
                    int index = i * numberOfColumns + j;

                    // generate card game object
                    string name = "Card" + index;
                    GameObject card = (GameObject)Instantiate(CardPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                    card.name = name;
                    card.transform.parent = transform;
                    card.transform.localScale = new Vector3(cardSize, cardSize, 1);

                    // initiate variable
                    if (k < difficultyLevel)
                    {
                        card.GetComponent<Card>().filled = true;
                    }

                    // assign position
                    card.transform.localPosition = SetCardPosition(index, i, j);

                    // assign orientation
                    card.transform.localEulerAngles = new Vector3(0, card.transform.localEulerAngles.y, 0);
                    if (layout != Layout.Flat)
                    {
                        GameObject center = new GameObject();
                        center.transform.SetParent(transform);
                        center.transform.localPosition = card.transform.localPosition;
                        center.transform.localPosition = new Vector3(0, center.transform.localPosition.y, 0);

                        card.transform.LookAt(center.transform.position);

                        card.transform.localEulerAngles += Vector3.up * 180;
                        Destroy(center);
                    }
                    cards.Add(card);

                    k++;
                }
            }
        }
        else {
            if (numberOfRows == 3 && numberOfColumns == 12)
            {
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        for (int h = 0; h < 4; h++)
                        {
                            int index = i * 12 + j * 4 + h;
                            // generate card game object
                            string name = "Card" + index;
                            GameObject card = (GameObject)Instantiate(CardPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                            card.name = name;
                            card.transform.parent = transform;
                            card.transform.localScale = new Vector3(cardSize, cardSize, 1);

                            // initiate variable
                            if (k < difficultyLevel)
                            {
                                card.GetComponent<Card>().filled = true;
                            }

                            // assign position
                            card.transform.localPosition = SetCardPositionInCube(index, h, j, i);

                            // assign orientation
                            card.transform.localEulerAngles = new Vector3(0, card.transform.localEulerAngles.y, 0);

                            cards.Add(card);

                            k++;
                        }
                    }
                }
            }
        }

        return cards;
    }

    // Shuffle Cards order
    private void ShuffleCards()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            GameObject temp = cards[i];
            int randomIndex = Random.Range(i, cards.Count);
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }

        SetCardsPositions(cards, layout);
    }

    // Set Cards Positions based on current layout
    private void SetCardsPositions(List<GameObject> localCards, Layout localLayout)
    {
        if (localLayout != Layout.Cube)
        {
            for (int i = 0; i < numberOfRows; i++)
            {
                for (int j = 0; j < numberOfColumns; j++)
                {
                    int index = i * numberOfColumns + j;
                    localCards[index].transform.localPosition = SetCardPosition(index, i, j);

                    localCards[index].transform.localEulerAngles = new Vector3(0, localCards[index].transform.localEulerAngles.y, 0);

                    if (localLayout != Layout.Flat)
                    {
                        if (localLayout == Layout.FullCircle)
                        {
                            transform.localPosition = new Vector3(0, 0.5f, 0);
                            GameObject.Find("PreferableStand").transform.localPosition = new Vector3(0, 0.01f, 0);
                        }
                        else
                        {
                            transform.localPosition = new Vector3(0, 0.5f, -1);
                            GameObject.Find("PreferableStand").transform.localPosition = new Vector3(0, 0.01f, -1);
                        }

                        GameObject center = new GameObject();
                        center.transform.SetParent(this.transform);
                        center.transform.localPosition = localCards[index].transform.localPosition;
                        center.transform.localPosition = new Vector3(0, center.transform.localPosition.y, 0);

                        localCards[index].transform.LookAt(center.transform.position);

                        localCards[index].transform.localEulerAngles += Vector3.up * 180;
                        Destroy(center);
                    }
                    else
                    {
                        transform.localPosition = new Vector3(0, 0.5f, -1);
                        GameObject.Find("PreferableStand").transform.localPosition = new Vector3(0, 0.01f, -1);
                        localCards[index].transform.localEulerAngles = new Vector3(0, 0, 0);
                    }
                }
            }
        }
        else {
            if (numberOfRows == 3 && numberOfColumns == 12) {
                for (int i = 0; i < 3; i++) {
                    for (int j = 0; j < 3; j++) {
                        for (int k = 0; k < 4; k++) {
                            int index = i * 12 + j * 4 + k;
                            localCards[index].transform.localPosition = SetCardPositionInCube(index, k, j, i);

                            localCards[index].transform.localEulerAngles = new Vector3(0, localCards[index].transform.localEulerAngles.y, 0);

                            transform.localPosition = new Vector3(0, 0.5f, -1);
                            GameObject.Find("PreferableStand").transform.localPosition = new Vector3(0, 0.01f, -1);
                            localCards[index].transform.localEulerAngles = new Vector3(0, 0, 0);
                        }
                    }
                }
            }
        }
        
    }

    // Set Card Position
    private Vector3 SetCardPosition(int index, int row, int col)
    {

        float xValue = 0;
        float yValue = 0;
        float zValue = 0;

        switch (layout)
        {
            case Layout.Flat:
                xValue = (index - (row * numberOfColumns) - (numberOfColumns / 2.0f - 0.5f)) * hDelta;
                yValue = (numberOfRows - (row + 1)) * vDelta;
                zValue = 2;
                break;
            case Layout.SemiCircle:
                xValue = -Mathf.Cos((index - (row * numberOfColumns)) * Mathf.PI / (numberOfColumns - 1)) * ((numberOfColumns - 1) * hDelta / Mathf.PI);
                yValue = (numberOfRows - (row + 1)) * vDelta;
                zValue = Mathf.Sin((index - (row * numberOfColumns)) * Mathf.PI / (numberOfColumns - 1)) * ((numberOfColumns - 1) * hDelta / Mathf.PI);
                break;
            case Layout.FullCircle:
                xValue = -Mathf.Cos((index - (row * numberOfColumns)) * Mathf.PI / (numberOfColumns / 2.0f)) * ((numberOfColumns - 1) * hDelta / (2.0f * Mathf.PI));
                yValue = (numberOfRows - (row + 1)) * vDelta;
                zValue = Mathf.Sin((index - (row * numberOfColumns)) * Mathf.PI / (numberOfColumns / 2.0f)) * ((numberOfColumns - 1) * hDelta / (2.0f * Mathf.PI));
                break;
            default:
                break;
        }

        return new Vector3(xValue, yValue, zValue);
    }

    // Set Card Position 3D
    private Vector3 SetCardPositionInCube(int index, int x, int y, int z)
    {

        float xValue = (x - 1.5f) * hDelta;
        float yValue = y * vDelta;
        float zValue = 0.5f + (z + 1) * hDelta;

        return new Vector3(xValue, yValue, zValue);
    }

    // Set Card Color
    private void SetCardsColor(Transform t, Color color) {
        if (color == Color.white)
        {
            t.GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(true);
        }
        else {
            t.GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false);
        }
        //t.GetChild(0).GetComponent<MeshRenderer>().material.color = color;
    }

    // Set Card Shape
    private void SetCardsShape(Transform t) {
        t.GetChild(0).GetComponent<MeshRenderer>().material.color = Color.white;
        t.GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(true);

        if (gameData == GameData.Shape)
        {
            t.GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetComponent<Image>().sprite = ShapePrefab1;
        }
    }

    // Check if card filled property is true
    private bool IsCardFilled(GameObject go) {
        if (go.GetComponent<Card>().filled)
            return true;
        return false;
    }

    // Check if card flipped property is true
    private bool IsCardFlipped(GameObject go)
    {
        if (go.GetComponent<Card>().flipped)
            return true;
        return false;
    }

    // Check if card flipped property is true
    private bool IsCardRotating(GameObject go)
    {
        if (go.GetComponent<Card>().rotating)
            return true;
        return false;
    }

    // timer function
    private void TimerAndCheckScan() {
        //// timer function
        //if (memoryTime >= 0 && startCount)
        //    memoryTime -= Time.deltaTime;

        //if (memoryTime < 0.5f)
        //    HidePattern();

        PrintTextToScreen(DashBoardText, "");

        if (MemoryTypeText.text == "") {
            if (memoryType == MemoryType.IKVS)
            {
                PrintTextToScreen(MemoryTypeText, "Please <color=red>select</color> and <color=red>remember</color> all the positions for <color=green>" + difficultyLevel + "</color> white cards.");
            }
            else if (memoryType == MemoryType.DKVS) {
                PrintTextToScreen(MemoryTypeText, "Please <color=red>touch</color> and <color=red>remember</color> all the positions for <color=green>" + difficultyLevel + "</color> white cards.");
            }
            else {
                PrintTextToScreen(MemoryTypeText, "Please <color=red>remember</color> all the positions for " + difficultyLevel + " white cards.");
            }
        }

        CheckEverythingScaned();
        CheckEverythingSelected();

        // assign left and right controllers interaction use
        if (mainHandCE != null)
        {
            if (mainHandCE.touchpadPressed)
            {
                localTouchpadPressed = true;
            }
            else
            {
                if (memoryType == MemoryType.VS)
                {
                    if (allSeen)
                    {
                        if (localTouchpadPressed)
                        {
                            HidePattern();
                        }
                    }
                }
                else
                {
                    if (allSeen && allSelected)
                    {
                        if (localTouchpadPressed)
                        {
                            HidePattern();
                        }
                    }
                }
                localTouchpadPressed = false;
            }
        }
    }

    // check user viewport
    private void CheckEverythingScaned() {
        if (scanTime >= 0 && startCount && !allSeen)
            scanTime += Time.deltaTime;

        allSeen = true;

        foreach (GameObject go in cards)
        {
            Vector3 wtvp = Camera.main.WorldToViewportPoint(go.transform.position);

            if (wtvp.x < 0.8f && wtvp.x > 0.2f && wtvp.y < 0.8f && wtvp.y > 0.2f && wtvp.z > 0f)
            {
                go.GetComponent<Card>().seen = true;
            }

            if (!go.GetComponent<Card>().seen)
            {
                allSeen = false;
            }
        }

        if (allSeen)
        {
            PrintTextToScreen(SeenCardText, "All cards have been seen (" + scanTime.ToString("#.0") + " s)");
            SeenCardText.color = Color.green;
        }
        else
        {
            PrintTextToScreen(SeenCardText, "You are missing some cards");
            SeenCardText.color = Color.red;
        }
    }

    // check user selected all filled cards
    private void CheckEverythingSelected()
    {
        if (selectTime >= 0 && startCount && !allSelected)
            selectTime += Time.deltaTime;

        allSelected = true;

        // assign left and right controllers interaction use
        if (mainHandIU == null)
        {
            if (controllerHand == 0 && GameObject.Find("LeftControllerAlias") != null)
                mainHandIU = GameObject.Find("LeftControllerAlias").GetComponent<VRTK_InteractUse>();
            else if (controllerHand == 1 && GameObject.Find("RightControllerAlias") != null)
                mainHandIU = GameObject.Find("RightControllerAlias").GetComponent<VRTK_InteractUse>();
        }

        if (mainHandIU.GetUsingObject() != null)
        {
            GameObject selectedCard = mainHandIU.GetUsingObject();
            selectedCard.GetComponent<Card>().selected = true;
        }

        foreach (GameObject go in cards)
        {
            if (IsCardFilled(go)) {
                if (!go.GetComponent<Card>().selected) {
                    allSelected = false;
                }
            }
        }


        if (memoryType == MemoryType.VS)
        {
            PrintTextToScreen(SelectCardText, "");
        }
        else {
            if (allSelected)
            {
                PrintTextToScreen(SelectCardText, "All cards have been selected (" + selectTime.ToString("#.0") + " s)");
                SelectCardText.color = Color.green;
            }
            else
            {
                PrintTextToScreen(SelectCardText, "Please select all white cards");
                SelectCardText.color = Color.red;
            }
        }
        
    }

    private void PrintTextToScreen(Text textBoard, string text) {
          textBoard.text = text;
    }

    // rotate coroutine with animation
    private IEnumerator Rotate(Transform rotateObject, Vector3 angles, float duration)
    {
        if (rotateObject != null)
        {
            rotateObject.GetComponent<Card>().rotating = true;
            rotateObject.GetComponent<VRTK_InteractableObject>().isUsable = false;
            Quaternion startRotation = rotateObject.rotation;
            Quaternion endRotation = Quaternion.Euler(angles) * startRotation;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                rotateObject.rotation = Quaternion.Lerp(startRotation, endRotation, t / duration);
                yield return null;
            }
            rotateObject.rotation = endRotation;
            rotateObject.GetComponent<VRTK_InteractableObject>().isUsable = true;

            rotateObject.GetComponent<Card>().rotating = false;
        }
    }

    /// <summary>
    /// Log related functions
    /// </summary>
    /// 

    //private string GetUserID()
    //{

    //}

    //private string GetTrialNumber()
    //{

    //}

    //private string GetTrialID() {

    //}

    //private string GetLayout()
    //{

    //}

    //private string GetMemoryType()
    //{

    //}

    //private string GetDifficultyLevel()
    //{

    //}

    //private string GetSeenTime()
    //{

    //}

    //private string GetSelectTime()
    //{

    //}

    public void QuitGame()
    {
        // save any game data here
#if UNITY_EDITOR
        // Application.Quit() does not work in the editor so
        // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
        UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif
    }
}
