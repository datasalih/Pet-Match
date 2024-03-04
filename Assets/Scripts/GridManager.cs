using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public GameManager gameManager; // Assign this in the Unity inspector

    public int width = 5;
    public int height = 10;
    public float spacing = 0.7f; // New variable to adjust the distance between candies
    [SerializeField] GameObject[] candyPrefabs;
    private GameObject[,] grid;

    private GameObject firstSelectedCandy;
    private Vector2Int firstSelectedPosition;
    private bool isSwapping = false;

    [SerializeField] float swapSpeed = 1f; // Control the speed of the swap
    List<Vector2Int> emptySpaces = new List<Vector2Int>();


    private GameObject matchSound;

    void Start()
    {

        matchSound = GameObject.Find("MatchSound");
        

        grid = new GameObject[width, height];
        FillGrid();
    }

    void FillGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                SpawnCandy(x, y);
            }
        }
    }

    void SpawnCandy(int x, int y)
    {
        List<GameObject> availableCandies = new List<GameObject>(candyPrefabs);
        if (x > 1)
        {
            // Check left for potential matches
            GameObject left1 = grid[x - 1, y];
            GameObject left2 = grid[x - 2, y];
            if (left1 != null && left2 != null && left1.tag == left2.tag)
            {
                // Remove the prefab that would create a match from the available list
                availableCandies.RemoveAll(candy => candy.tag == left1.tag);
            }
        }

        if (y > 1)
        {
            // Check above for potential matches
            GameObject above1 = grid[x, y - 1];
            GameObject above2 = grid[x, y - 2];
            if (above1 != null && above2 != null && above1.tag == above2.tag)
            {
                // Remove the prefab that would create a match from the available list
                availableCandies.RemoveAll(candy => candy.tag == above1.tag);
            }
        }

        if (availableCandies.Count == 0)
        {
            // In the unlikely event that no candies are available (which can happen if you have very few candy types),
            // revert to using the full list to prevent an infinite loop.
            // Consider adjusting your game design to ensure this doesn't happen.
            availableCandies = new List<GameObject>(candyPrefabs);
        }

        int randomIndex = Random.Range(0, availableCandies.Count);
        GameObject selectedCandy = Instantiate(availableCandies[randomIndex], new Vector3(x * spacing, y * spacing, 0), Quaternion.identity);
        selectedCandy.transform.parent = this.transform;
        grid[x, y] = selectedCandy;
    }


    void Update()
    {
        // Ignore inputs if a swap is currently in progress
        if (isSwapping)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            SelectCandy();
        }
        else if (Input.GetMouseButtonUp(0) && firstSelectedCandy != null)
        {
            TrySwapCandies();
        }
    }


    void SelectCandy()
    {
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);

        if (hit.collider != null && hit.collider.gameObject.GetComponent<BoxCollider2D>() != null)
        {
            firstSelectedCandy = hit.collider.gameObject;
            firstSelectedPosition = new Vector2Int(Mathf.RoundToInt(firstSelectedCandy.transform.position.x / spacing), Mathf.RoundToInt(firstSelectedCandy.transform.position.y / spacing));
        }
    }

    void TrySwapCandies()
    {
        if (isSwapping) return; // Prevent new swaps if already swapping

        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);

        if (hit.collider != null && hit.collider.gameObject.GetComponent<BoxCollider2D>() != null)
        {
            GameObject secondSelectedCandy = hit.collider.gameObject;
            Vector2Int secondSelectedPosition = new Vector2Int(Mathf.RoundToInt(secondSelectedCandy.transform.position.x / spacing), Mathf.RoundToInt(secondSelectedCandy.transform.position.y / spacing));

            if ((firstSelectedPosition.x == secondSelectedPosition.x || firstSelectedPosition.y == secondSelectedPosition.y) &&
                (Mathf.Abs(firstSelectedPosition.x - secondSelectedPosition.x) == 1 || Mathf.Abs(firstSelectedPosition.y - secondSelectedPosition.y) == 1))
            {
                StartCoroutine(SwapCandies(firstSelectedPosition, secondSelectedPosition, firstSelectedCandy, secondSelectedCandy));
            }
            else
            {
                firstSelectedCandy = null; // Deselect if not a valid move
            }
        }
    }




    IEnumerator SwapCandies(Vector2Int pos1, Vector2Int pos2, GameObject candy1, GameObject candy2)
    {
        isSwapping = true;
        gameManager.UseMove();

        // Perform the swap in the grid data structure immediately
        grid[pos1.x, pos1.y] = candy2;
        grid[pos2.x, pos2.y] = candy1;

        // Animate the swap
        float elapsedTime = 0f;
        Vector3 startPosition1 = candy1.transform.position;
        Vector3 startPosition2 = candy2.transform.position;

        while (elapsedTime < swapSpeed)
        {
            elapsedTime += Time.deltaTime;
            candy1.transform.position = Vector3.Lerp(startPosition1, startPosition2, elapsedTime / swapSpeed);
            candy2.transform.position = Vector3.Lerp(startPosition2, startPosition1, elapsedTime / swapSpeed);
            yield return null;
        }

        // Update actual positions after animation
        candy1.transform.position = startPosition2;
        candy2.transform.position = startPosition1;

        // Check for matches after the swap
        if (!CheckForMatches())
        {
            // If no match found, revert the swap in the grid and animate swap back
            grid[pos1.x, pos1.y] = candy1;
            grid[pos2.x, pos2.y] = candy2;

            // Animate swap back
            elapsedTime = 0; // Reset time for the swap back animation
            while (elapsedTime < swapSpeed)
            {
                elapsedTime += Time.deltaTime;
                candy1.transform.position = Vector3.Lerp(startPosition2, startPosition1, elapsedTime / swapSpeed);
                candy2.transform.position = Vector3.Lerp(startPosition1, startPosition2, elapsedTime / swapSpeed);
                yield return null;
            }

            // Reset positions after swap back animation
            candy1.transform.position = startPosition1;
            candy2.transform.position = startPosition2;
        }

        isSwapping = false; // Allow new inputs after swapping is done or reverted
    }




    bool CheckForMatches()
    {
        bool matchFound = false;
        List<GameObject> candiesToClear = new List<GameObject>();

        // Horizontal Checks
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 2; x++)
            {
                if (grid[x, y] != null && grid[x + 1, y] != null && grid[x + 2, y] != null)
                {
                    if (grid[x, y].tag == grid[x + 1, y].tag && grid[x, y].tag == grid[x + 2, y].tag)
                    {
                        matchFound = true;
                        gameManager.AddScore(30);
                        matchSound.GetComponent<AudioSource>().Play();

                        if (!candiesToClear.Contains(grid[x, y])) candiesToClear.Add(grid[x, y]);
                        if (!candiesToClear.Contains(grid[x + 1, y])) candiesToClear.Add(grid[x + 1, y]);
                        if (!candiesToClear.Contains(grid[x + 2, y])) candiesToClear.Add(grid[x + 2, y]);
                    }
                }
            }
        }

        // Vertical Checks
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 2; y++)
            {
                if (grid[x, y] != null && grid[x, y + 1] != null && grid[x, y + 2] != null)
                {
                    if (grid[x, y].tag == grid[x, y + 1].tag && grid[x, y].tag == grid[x, y + 2].tag)
                    {
                        matchFound = true;
                        gameManager.AddScore(30);
                        matchSound.GetComponent<AudioSource>().Play();  

                        if (!candiesToClear.Contains(grid[x, y])) candiesToClear.Add(grid[x, y]);
                        if (!candiesToClear.Contains(grid[x, y + 1])) candiesToClear.Add(grid[x, y + 1]);
                        if (!candiesToClear.Contains(grid[x, y + 2])) candiesToClear.Add(grid[x, y + 2]);
                    }
                }
            }
        }

        // Clearing Matches
        if (matchFound)
        {
            foreach (GameObject candy in candiesToClear)
            {
                Vector2Int pos = FindCandyGridPosition(candy);
                Destroy(candy);
                grid[pos.x, pos.y] = null;
                emptySpaces.Add(pos); // Track empty spaces for refilling
            }
            AfterMatchProcedure(); // Refill grid after clearing matches
        }

        return matchFound;
    }


    void AfterMatchProcedure()
    {
        FillEmptySpaces();
    }



    Vector2Int FindCandyGridPosition(GameObject candy)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == candy)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return new Vector2Int(-1, -1); // Indicate not found
    }


    void FillEmptySpaces()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null) // If space is empty
                {
                    SpawnCandy(x, y);
                }
            }
        }

        // After filling, automatically check for and clear any matches.
        StartCoroutine(CheckAndClearMatches());
    }

    IEnumerator CheckAndClearMatches()
    {
        yield return new WaitForSeconds(0.5f); // Wait for new candies to settle.

        bool foundMatches = false;
        do
        {
            foundMatches = CheckForMatches(); // Assume this checks and also clears the matches.

            if (foundMatches)
            {
                yield return new WaitForSeconds(0.5f); // Wait for matches to be cleared and candies to settle.
                FillNewCandies();
                yield return new WaitForSeconds(0.5f); // Wait for new candies to settle.
            }
        } while (foundMatches); // Repeat if new matches were found and cleared.
    }

    void FillNewCandies()
    {
        // Similar to FillEmptySpaces but might include animations or delays as candies "fall in" from the top.
        for (int x = 0; x < width; x++)
        {
            int emptyCountAtColumn = 0;
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null)
                {
                    emptyCountAtColumn++;
                    SpawnCandy(x, y + emptyCountAtColumn); // Adjust spawn position based on how many empties are found.
                                                           // Adjust if you're implementing animations for candies falling into place.
                }
            }
        }
    }



}

