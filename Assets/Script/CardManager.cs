using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using TMPro;
using UnityEngine.UI;

public enum Layout
{
    Flat,    
    SemiCircle, 
    FullCircle,
    Cube
}

public enum GameType
{
    Pair,
    Recall
}

public enum GameData
{
    Letter,
    Shape,
    Color
}

public class CardManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject CardPrefab;
    public GameObject fireworksEffect1;
    public GameObject fireworksEffect2;
    public Sprite ShapePrefab1;
    public Sprite ShapePrefab2;
    public Sprite ShapePrefab3;
    public Sprite ShapePrefab4;
    public Sprite ShapePrefab5;
    public Sprite ShapePrefab6;
    public Sprite ShapePrefab7;
    public Sprite ShapePrefab8;
    public Sprite ShapePrefab9;

    [Header("Predefined Variables")]
    public float hDelta;
    public float vDelta;
    public float cardSize;

    [Header("Variables")]
    public int numberOfRows;
    public int numberOfColumns;
    public int workingMemoryLoad;
    public Layout layout;
    public GameType gameType;
    public GameData gameData;
    public int experimentNo;

    // local variables
    private List<GameObject> cards;
    private List<GameObject> fireworks;
    private List<Sprite> shapes;
    private GameObject oddItem;
    private VRTK_InteractUse leftIU;
    private VRTK_InteractUse rightIU;
    private VRTK_ControllerEvents leftCE;
    private VRTK_ControllerEvents rightCE;
    private GameObject firstCard;
    private GameObject secondCard;
    private GameObject oldFirstCard; // delay rotation
    private GameObject oldSecondCard; // delay rotation
    private bool finished = false;
    private int completedPair = 0;

    static readonly string[] Columns = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P",
            "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "AA", "AB", "AC", "AD", "AE", "AF", "AG", "AH", "AI", "AJ", "AK", "AL",
            "AM", "AN", "AO", "AP", "AQ", "AR", "AS", "AT", "AU", "AV", "AW", "AX", "AY", "AZ", "BA", "BB", "BC", "BD", "BE", "BF",
            "BG", "BH", "BI", "BJ", "BK", "BL", "BM", "BN", "BO", "BP", "BQ", "BR", "BS", "BT", "BU", "BV", "BW"}; // 150

    // Start is called before the first frame update
    void Start()
    {
        // initialise variables
        cards = new List<GameObject>();
        fireworks = new List<GameObject>();
        shapes = new List<Sprite>();
        shapes.Add(ShapePrefab1);
        shapes.Add(ShapePrefab2);
        shapes.Add(ShapePrefab3);
        shapes.Add(ShapePrefab4);
        shapes.Add(ShapePrefab5);
        shapes.Add(ShapePrefab6);
        shapes.Add(ShapePrefab7);
        shapes.Add(ShapePrefab8);
        shapes.Add(ShapePrefab9);

        if (gameData == GameData.Shape) {
            if (workingMemoryLoad > 9 || workingMemoryLoad <= 0)
                workingMemoryLoad = 9;
        }
            

        // generate and shuffle cards
        cards = GenerateCards();
        ShuffleCards();
    }

    // Update is called once per frame
    void Update()
    {
        // change layout shortcut
        if (Input.GetKeyDown("c"))
            Changelayout();

        // restart game shortcut
        if (Input.GetKeyDown("r"))
            RestartGame();

        // main game logic
        if (gameType == GameType.Pair)
            PairGameInteraction();
 
        else if (gameType == GameType.Recall)
            RecallGameInteraction();

        // check finish game
        if (finished)
            CheckRestart();
    }

    // Change Layout
    private void Changelayout() {
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

        SetCardsPositions(cards, layout);
    } 

    // Generate Cards
    private List<GameObject> GenerateCards() {
        List<GameObject> cards = new List<GameObject>();
        int k = 0;

        for (int i = 0; i < numberOfRows; i++) {
            for (int j = 0; j < numberOfColumns; j++)
            {
                int index = i * numberOfColumns + j;
                string name = "Card" + index;
                GameObject card = (GameObject)Instantiate(CardPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                card.name = name;
                card.transform.parent = transform;
                card.transform.localScale = new Vector3(cardSize, cardSize, 1);

                TextMeshProUGUI tmp = card.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();

                if (gameType == GameType.Pair)
                {
                    if (k < workingMemoryLoad * 2 || workingMemoryLoad == 0)
                    {
                        card.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = Color.white;
                        if (gameData == GameData.Letter)
                            tmp.text = GetLetterForCards(index);
                        else if (gameData == GameData.Shape)
                        {
                            card.transform.GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(true);
                            card.transform.GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetComponent<Image>().sprite = GetShapeForCards(index);
                        }
                    }
                }
                else if(gameType == GameType.Recall) {
                    if (k < workingMemoryLoad || workingMemoryLoad == 0)
                    {
                        card.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = Color.white;
                        if (gameData == GameData.Letter)
                            tmp.text = GetLetterForCards(index);
                        else if (gameData == GameData.Shape)
                        {
                            card.transform.GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(true);
                            card.transform.GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetComponent<Image>().sprite = GetShapeForCards(index);
                        }
                    }
                }

                // assign position
                card.transform.localPosition = SetCardPosition(index, i, j);

                // assign orientation
                card.transform.localEulerAngles = new Vector3(0, card.transform.localEulerAngles.y, 0);
                if (layout != Layout.Flat && layout != Layout.Cube)
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

        // get odd item
        if ((workingMemoryLoad == 0 || workingMemoryLoad >= (numberOfRows * numberOfColumns)) && ((numberOfRows * numberOfColumns) % 2 == 1))
            oddItem = cards[cards.Count - 1];
        else
            oddItem = null;

        return cards;
    }

    // Shuffle Cards
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
    private void SetCardsPositions(List<GameObject> localCards, Layout localLayout) {
        for (int i = 0; i < numberOfRows; i++)
        {
            for (int j = 0; j < numberOfColumns; j++)
            {
                int index = i * numberOfColumns + j;
                localCards[index].transform.localPosition = SetCardPosition(index, i, j);

                localCards[index].transform.localEulerAngles = new Vector3(0, localCards[index].transform.localEulerAngles.y, 0);

                if (localLayout != Layout.Flat)
                {
                    GameObject center = new GameObject();
                    center.transform.SetParent(this.transform);
                    center.transform.localPosition = localCards[index].transform.localPosition;
                    center.transform.localPosition = new Vector3(0, center.transform.localPosition.y, 0);

                    localCards[index].transform.LookAt(center.transform.position);

                    localCards[index].transform.localEulerAngles += Vector3.up * 180;
                    Destroy(center);
                }
                else
                    localCards[index].transform.localEulerAngles = new Vector3(0, 0, 0);
            }
        }
    }

    // Set Card Position
    private Vector3 SetCardPosition(int index, int row, int col){

        float xValue = 0;
        float yValue = 0;
        float zValue = 0;

        switch (layout) {
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

    ///////////////////////////////////////////////////////
    /// Card Content
    ///////////////////////////////////////////////////////

    // Get Text on Cards
    private string GetLetterForCards(int index) {
        if (gameType == GameType.Pair)
        {
            return Columns[index / 2];
        }
        else if (gameType == GameType.Recall)
        {
            return Columns[index];
        }
        else
            return "";
    }

    // Get Shape on Cards
    private Sprite GetShapeForCards(int index)
    {
        if (gameType == GameType.Pair)
        {
            return shapes[index / 2];
        }
        else if (gameType == GameType.Recall)
        {
            return shapes[index];
        }
        else
            return null;
    }

    ///////////////////////////////////////////////////////////////////////
    /// Game Logic
    ///////////////////////////////////////////////////////////////////////

    // check card interaction with pointer (Pair game)
    private void PairGameInteraction() {

        // assign left and right controllers interaction use
        if (leftIU == null)
            if (GameObject.Find("LeftControllerAlias") != null)
                leftIU = GameObject.Find("LeftControllerAlias").GetComponent<VRTK_InteractUse>();

        if (rightIU == null)
            if (GameObject.Find("RightControllerAlias") != null)
                rightIU = GameObject.Find("RightControllerAlias").GetComponent<VRTK_InteractUse>();

        // assign first and second cards when used by the controller
        if (firstCard == null)
        {
            if (leftIU.GetUsingObject() != null)
            {
                if (leftIU.GetUsingObject() != oldFirstCard && leftIU.GetUsingObject() != oldSecondCard)
                {
                    if (oldFirstCard != null && oldSecondCard != null)
                    { // delay rotation for old two cards
                        StartCoroutine(Rotate(oldFirstCard.transform, new Vector3(0, 180, 0), 1f, false));
                        StartCoroutine(Rotate(oldSecondCard.transform, new Vector3(0, 180, 0), 1f, false));
                    }

                    firstCard = leftIU.GetUsingObject();
                    StartCoroutine(Rotate(firstCard.transform, new Vector3(0, 180, 0), 0.5f, true));
                }
                else {
                    if (leftIU.GetUsingObject() == oldFirstCard)
                    {
                        StartCoroutine(Rotate(oldSecondCard.transform, new Vector3(0, 180, 0), 1f, false));
                    }
                    else if (leftIU.GetUsingObject() == oldSecondCard) {
                        StartCoroutine(Rotate(oldFirstCard.transform, new Vector3(0, 180, 0), 1f, false));
                    }
                    firstCard = leftIU.GetUsingObject();
                }
            }
            else if (rightIU.GetUsingObject() != null)
            {
                if (rightIU.GetUsingObject() != oldFirstCard && rightIU.GetUsingObject() != oldSecondCard)
                {
                    if (oldFirstCard != null && oldSecondCard != null)
                    { // delay rotation for old two cards
                        StartCoroutine(Rotate(oldFirstCard.transform, new Vector3(0, 180, 0), 1f, false));
                        StartCoroutine(Rotate(oldSecondCard.transform, new Vector3(0, 180, 0), 1f, false));
                    }

                    firstCard = rightIU.GetUsingObject();
                    StartCoroutine(Rotate(firstCard.transform, new Vector3(0, 180, 0), 0.5f, true));
                }
                else {
                    if (rightIU.GetUsingObject() == oldFirstCard)
                    {
                        StartCoroutine(Rotate(oldSecondCard.transform, new Vector3(0, 180, 0), 1f, false));
                    }
                    else if (rightIU.GetUsingObject() == oldSecondCard)
                    {
                        StartCoroutine(Rotate(oldFirstCard.transform, new Vector3(0, 180, 0), 1f, false));
                    }
                    firstCard = rightIU.GetUsingObject();
                }
            }
        }
        else if (firstCard != null && secondCard == null)
        {
            if (leftIU.GetUsingObject() != null && leftIU.GetUsingObject() != firstCard)
            {
                secondCard = leftIU.GetUsingObject();
                StartCoroutine(Rotate(secondCard.transform, new Vector3(0, 180, 0), 0.5f, true));
            }
            else if (rightIU.GetUsingObject() != null && rightIU.GetUsingObject() != firstCard)
            {
                secondCard = rightIU.GetUsingObject();
                StartCoroutine(Rotate(secondCard.transform, new Vector3(0, 180, 0), 0.5f, true));
            }
        }

        // main game logic to check if the text is the same
        if (firstCard != null && secondCard != null) {
            if (firstCard.GetComponent<Card>().flipped && secondCard.GetComponent<Card>().flipped)
            {
                // if odd item, remove interactable ability
                if (firstCard == oddItem)
                {
                    firstCard.GetComponent<VRTK_InteractableObject>().isUsable = false;
                    firstCard = null;
                    StartCoroutine(Rotate(secondCard.transform, new Vector3(0, 180, 0), 1f, false));
                    secondCard = null;
                }
                else if (secondCard == oddItem)
                {
                    secondCard.GetComponent<VRTK_InteractableObject>().isUsable = false;
                    secondCard = null;
                    StartCoroutine(Rotate(firstCard.transform, new Vector3(0, 180, 0), 1f, false));
                    firstCard = null;
                }
                else {
                    if (gameData == GameData.Letter)
                    {
                        if (firstCard.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text ==
                                    secondCard.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text)
                        {
                            oldFirstCard = null;
                            oldSecondCard = null;
                            firstCard.GetComponent<VRTK_InteractableObject>().isUsable = false;
                            secondCard.GetComponent<VRTK_InteractableObject>().isUsable = false;
                            firstCard = null;
                            secondCard = null;
                            completedPair++;
                        }
                        else
                        {
                            oldFirstCard = firstCard;
                            oldSecondCard = secondCard;
                            firstCard = null;
                            secondCard = null;
                        }
                    }
                    else if (gameData == GameData.Shape) {
                        Sprite fstSprite = firstCard.transform.GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetComponent<Image>().sprite;
                        Sprite secSprite = secondCard.transform.GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetComponent<Image>().sprite;
                        if (fstSprite != null && secSprite != null && fstSprite == secSprite)
                        {
                            oldFirstCard = null;
                            oldSecondCard = null;
                            firstCard.GetComponent<VRTK_InteractableObject>().isUsable = false;
                            secondCard.GetComponent<VRTK_InteractableObject>().isUsable = false;
                            firstCard = null;
                            secondCard = null;
                            completedPair++;
                        }
                        else
                        {
                            oldFirstCard = firstCard;
                            oldSecondCard = secondCard;
                            firstCard = null;
                            secondCard = null;
                        }
                    }
                    
                }

                CheckPairCompletion();
            }
        }
    }

    // check card interaction (recall game)
    private void RecallGameInteraction() {

    }


    // check if the game is completed for pairGame
    private void CheckPairCompletion() {
        if (workingMemoryLoad == 0 || workingMemoryLoad * 2 > numberOfRows * numberOfColumns)
        {
            bool complete = true;
            foreach (GameObject go in cards)
            {
                if (!go.GetComponent<Card>().flipped)
                {
                    complete = false;
                    break;
                }
            }

            if (complete)
            {
                finished = true;
                CongratulationF();
            }
        }
        else {
            if (completedPair == workingMemoryLoad)
            {
                finished = true;
                CongratulationF();
            }
        }
    }

    private void CheckRestart() {
        // assign left and right controllers controller events
        if (leftCE == null)
            if (GameObject.Find("LeftControllerAlias") != null)
                leftCE = GameObject.Find("LeftControllerAlias").GetComponent<VRTK_ControllerEvents>();

        if (rightCE == null)
            if (GameObject.Find("RightControllerAlias") != null)
                rightCE = GameObject.Find("RightControllerAlias").GetComponent<VRTK_ControllerEvents>();

        if (leftCE != null && rightCE != null) {
            if (leftCE.AnyButtonPressed() || rightCE.AnyButtonPressed()) {
                RestartGame();
            }
        }
    }

    // congrats program
    private void CongratulationF() {
        int fireworksNo = cards.Count / 4;
        int i = 0;
        while (i < fireworksNo) {
            int randomNo = Random.Range(0, cards.Count - 1);
            GameObject card = GameObject.Find("Card" + randomNo);
            if (card.transform.Find("firework") == null)
            {
                i++;
                GameObject firework = (GameObject)Instantiate(fireworksEffect1, new Vector3(0, 0, 0), Quaternion.identity);
                fireworks.Add(firework);
                firework.name = "firework";
                firework.transform.SetParent(card.transform);
                if (firework.transform.parent.localEulerAngles.y == 180)
                    firework.transform.localPosition = new Vector3(0, 0, 0.5f);
                else
                    firework.transform.localPosition = new Vector3(0, 0, -0.5f);
            }
        }

        int j = 0;
        while (j < fireworksNo)
        {
            int randomNo = Random.Range(0, cards.Count - 1);
            GameObject card = GameObject.Find("Card" + randomNo);
            if (card.transform.Find("firework") == null)
            {
                j++;
                GameObject firework = (GameObject)Instantiate(fireworksEffect2, new Vector3(0, 0, 0), Quaternion.identity);
                fireworks.Add(firework);
                firework.name = "firework";
                firework.transform.SetParent(card.transform);
                if (firework.transform.parent.localEulerAngles.y == 180)
                    firework.transform.localPosition = new Vector3(0, 0, 0.5f);
                else
                    firework.transform.localPosition = new Vector3(0, 0, -0.5f);
            }
        }
    }

    // rotate coroutine with animation
    private IEnumerator Rotate(Transform rotateObject, Vector3 angles, float duration, bool flipped)
    {
        if (rotateObject != null) {
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

            if (flipped)
                rotateObject.GetComponent<Card>().flipped = true;
            else
                rotateObject.GetComponent<Card>().flipped = false;
        }   
    }

    // restart a new game
    public void RestartGame() {
        if (cards != null) {
            finished = false;
            oldFirstCard = null;
            oldSecondCard = null;
            firstCard = null;
            secondCard = null;
            completedPair = 0;

            foreach (GameObject go in cards)
                Destroy(go);
            cards.Clear();

            foreach (GameObject go in fireworks)
                Destroy(go);
            fireworks.Clear();

            cards = GenerateCards();
            ShuffleCards();
        }
    }

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
