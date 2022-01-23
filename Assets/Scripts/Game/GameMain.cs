using JamKit;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game
{
    public static class GGJ21Extensions
    {
        public static Vector3 WorldPos(this Vector2Int v)
        {
            return new Vector3(v.x + 0.5f, v.y + 0.5f, 0);
        }

        public static Color ToColor(this CellType type)
        {
            switch (type)
            {
                case CellType.White:
                    ColorUtility.TryParseHtmlString("#f8f9fa", out Color c);
                    return c;
                case CellType.Black:
                    //ColorUtility.TryParseHtmlString("#212529", out c);
                    ColorUtility.TryParseHtmlString("#121212", out c);
                    return c;
                case CellType.Wall:
                    ColorUtility.TryParseHtmlString("#495057", out c);
                    return c;
            }

            return Color.magenta;
        }

        public static Vector2Int RotateCW(this Vector2Int v)
        {
            Vector2 rotated = Quaternion.AngleAxis(-90f, Vector3.forward) * (Vector2)v;
            return new Vector2Int(Mathf.RoundToInt(rotated.x), Mathf.RoundToInt(rotated.y));

        }

        public static CellType ToCellType(this Color c)
        {
            if (c == Color.white)
            {
                return CellType.White;
            }
            if (c == Color.black)
            {
                return CellType.Black;
            }
            if (c == Color.red)
            {
                return CellType.Wall;
            }
            if (c == Color.green)
            {
                return CellType.White;
            }

            throw new Exception($"Shouldn't be the case: {c}");
        }
    }

    public enum CellType
    {
        White, Black, Wall
    }

    public class Cell
    {
        public Vector2Int Pos;
        public CellType Type;
    }

    public class Token
    {
        public Vector2Int Pos;
        public Vector2Int Facing;
    }

    public class GameMain : MonoBehaviour
    {
        [SerializeField] private GameObject _cellPrefab;
        [SerializeField] private Transform _cellsParent;
        [SerializeField] private GameObject _tokenPrefab;
        [SerializeField] private Texture2D[] _levels;

        [SerializeField] private GameObject _restartButton;
        [SerializeField] private GameObject _successIcon;
        [SerializeField] private GameObject _nextButton;
        [SerializeField] private TextMeshProUGUI _inputCountText;
        [SerializeField] private TextMeshProUGUI _levelStartText;
        [SerializeField] private TextMeshProUGUI _levelFinishText;

        private int _gridSize;
        private Cell[][] _cells;
        private readonly Dictionary<Cell, GameObject> _cellViews = new Dictionary<Cell, GameObject>();

        private readonly List<Token> _tokens = new List<Token>();
        private readonly Dictionary<Token, GameObject> _tokenViews = new Dictionary<Token, GameObject>();

        private bool _isTokenMoving = false;
        private float _movementDurationPerCell = 0.25f;
        private float _movementStartDelay = 0.3f;

        private int _currentLevel = 0;
        private int _currentLevelInputCount = 0;
        private int _currentlevelInputLimit = 0;
        private bool _isLevelCompleted = false;
        private bool _isLevelTransitionStarted = false;

        private int _cameraMovementOffset = 10;

        private void Start()
        {
            CoroutineStarter.Run(LoadLevel(_currentLevel));
        }

        private IEnumerator LoadLevel(int levelIndex)
        {
            Debug.Assert(_levels[levelIndex].width == _levels[levelIndex].height, "Level texture should be square");
            _gridSize = _levels[levelIndex].width;
            _currentlevelInputLimit = Int32.Parse(_levels[levelIndex].name.Split('_')[1]);

            yield return Curve.TweenCoroutine(AnimationCurve.EaseInOut(0, 0, 1, 1), _movementDurationPerCell,
                t =>
                {
                    _cellsParent.position = Vector3.Lerp(Vector3.zero, new Vector3(-_cameraMovementOffset, 0, 0), t);
                });
            
            foreach (var cellPair in _cellViews)
            {
                Destroy(cellPair.Value);
            }
            _cellViews.Clear();
            foreach (var tokenPair in _tokenViews)
            {
                Destroy(tokenPair.Value);
            }
            _tokenViews.Clear();
            _tokens.Clear();
            
            Camera.main.transform.position = new Vector3(_gridSize / 2f - _cameraMovementOffset, _gridSize / 2f, -1);
            _cellsParent.position = Vector3.zero;

            _cells = new Cell[_gridSize][];
            for (int i = 0; i < _gridSize; i++)
            {
                _cells[i] = new Cell[_gridSize];
                for (int j = 0; j < _gridSize; j++)
                {
                    Vector2Int pos = new Vector2Int(i, j);

                    Color texPixel = _levels[levelIndex].GetPixel(i, j);
                    if (texPixel == Color.green)
                    {
                        Token tempToken = new Token() { Pos = pos, Facing = Vector2Int.up };
                        _tokens.Add(tempToken);
                        GameObject tokenGo = Instantiate(_tokenPrefab, pos.WorldPos(), Quaternion.identity);
                        tokenGo.transform.SetParent(_cellsParent);
                        _tokenViews.Add(tempToken, tokenGo);
                    }

                    CellType type = texPixel.ToCellType();

                    Cell cell = new Cell() { Pos = pos, Type = type };
                    _cells[i][j] = cell;

                    GameObject go = Instantiate(_cellPrefab, pos.WorldPos(), Quaternion.identity);
                    go.transform.SetParent(_cellsParent);
                    go.GetComponent<SpriteRenderer>().color = type.ToColor();

                    _cellViews.Add(cell, go);
                }
            }
            
            var targetCameraPosition = new Vector3(_gridSize / 2f, _gridSize / 2f, -1);
            var currentCameraPosition = new Vector3(_gridSize / 2f - _cameraMovementOffset, _gridSize / 2f, -1);
            
            Camera.main.transform.position = currentCameraPosition;
            yield return Curve.TweenCoroutine(AnimationCurve.EaseInOut(0, 0, 1, 1), _movementDurationPerCell,
                t =>
                {
                    Camera.main.transform.position = Vector3.Lerp(currentCameraPosition, targetCameraPosition, t);
                });
            Camera.main.transform.position = targetCameraPosition;

            _inputCountText.gameObject.SetActive(true);
            _inputCountText.text = _currentlevelInputLimit.ToString();
            _restartButton.SetActive(false);
            _successIcon.SetActive(false);
            _nextButton.SetActive(false);
            _levelFinishText.gameObject.SetActive(false);
            
            _isLevelCompleted = false;
            _currentLevelInputCount = 0;
            _isLevelTransitionStarted = false;
        }

        private void Update()
        {
            if (_isTokenMoving || _isLevelTransitionStarted)
            {
                return;
            }
            
            if (Input.GetMouseButtonDown(0) && !_isLevelCompleted)
            {
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
                {
                    foreach (var pair in _tokenViews)
                    {
                        if (pair.Value == hit.transform.gameObject)
                        {
                            OnTokenClicked(pair.Key);
                        }
                    }
                }
            }
            else if (_isLevelCompleted)
            {
                _currentLevel++;
                if (_currentLevel >= _levels.Length)
                {
                    SceneManager.LoadScene("End");
                }
                else
                {
                    _isLevelTransitionStarted = true;
                    var nextButtonImage = _nextButton.GetComponent<Image>();
                    _levelFinishText.gameObject.SetActive(true);
                    _nextButton.SetActive(true);
                    Curve.Tween(AnimationCurve.EaseInOut(0, 0, 1, 1), 1,
                        t =>
                        {
                            _levelFinishText.color = Color.Lerp(new Color(255,255,255,0), Color.white, t);
                            nextButtonImage.color = Color.Lerp(new Color(255,255,255,0), Color.white, t);
                        },
                        () =>
                        {
                            _levelFinishText.color = Color.white; 
                            nextButtonImage.color = Color.white;
                        });
                }
            }
        }

        private void OnTokenClicked(Token clickedToken)
        {
            Vector2Int dir = GetWalkDir(clickedToken);
            if (dir == Vector2Int.zero)
            {
                return;
            }
            Sfx.Instance.Play("ClickToken");
            clickedToken.Facing = dir;

            Vector2Int tokenTarget = clickedToken.Pos + dir;
            List<Cell> cellsToInvert = new List<Cell>();
            while (true)
            {
                Cell possibleTarget = CellAt(tokenTarget);
                if (IsPosWalkable(tokenTarget))
                {
                    cellsToInvert.Add(possibleTarget);
                    tokenTarget += dir;
                }
                else
                {
                    tokenTarget -= dir;
                    break;
                }
            }

            clickedToken.Pos = tokenTarget;

            // Invert cells
            for (int i = 0; i < cellsToInvert.Count; i++)
            {
                if (cellsToInvert[i].Type == CellType.White)
                {
                    ConvertCellView(cellsToInvert[i], CellType.White, CellType.Black, i);
                    cellsToInvert[i].Type = CellType.Black;
                }
                else if (cellsToInvert[i].Type == CellType.Black)
                {
                    ConvertCellView(cellsToInvert[i], CellType.Black, CellType.White, i);
                    cellsToInvert[i].Type = CellType.White;
                }
                else Debug.LogError("Walls shouldn't come here");
            }

            MoveTokenView(clickedToken, tokenTarget, cellsToInvert.Count);
            
            // Check if level completed
            bool allWhite = true;
            bool allBlack = true;
            foreach (var row in _cells)
            {
                foreach (var cell in row)
                {
                    allWhite &= (cell.Type == CellType.White || cell.Type == CellType.Wall);
                    allBlack &= (cell.Type == CellType.Black || cell.Type == CellType.Wall);
                }
            }

            _isLevelCompleted = allBlack || allWhite;

            _currentLevelInputCount++;
            _inputCountText.text = (_currentlevelInputLimit - _currentLevelInputCount).ToString();
            _inputCountText.gameObject.SetActive(!_isLevelCompleted && _currentlevelInputLimit > _currentLevelInputCount);
            _successIcon.gameObject.SetActive(_isLevelCompleted);
            _restartButton.SetActive(!_isLevelCompleted && _currentlevelInputLimit <= _currentLevelInputCount);
        }

        private Vector2Int GetWalkDir(Token token)
        {
            Vector2Int dir = token.Facing;

            for (int i = 0; i < 4; i++)
            {
                if (IsPosWalkable(token.Pos + dir))
                {
                    return dir;
                }

                dir = dir.RotateCW();
            }

            return Vector2Int.zero;
        }

        private void ConvertCellView(Cell cell, CellType from, CellType to, int index)
        {
            Color fromColor = from.ToColor();
            Color toColor = to.ToColor();
            SpriteRenderer sr = _cellViews[cell].GetComponent<SpriteRenderer>();

            CoroutineStarter.RunDelayed(_movementStartDelay + index * _movementDurationPerCell, () =>
            {
                Sfx.Instance.Play("InvertCell");
                Curve.Tween(AnimationCurve.EaseInOut(0, 0, 1, 1), _movementDurationPerCell,
                    t => { sr.color = Color.Lerp(fromColor, toColor, t); },
                    () => { sr.color = toColor; });
            });
        }

        private Cell CellAt(Vector2Int pos)
        {
            if (pos.x < 0 || pos.x >= _gridSize ||
                pos.y < 0 || pos.y >= _gridSize)
            {
                return null;
            }

            return _cells[pos.x][pos.y];
        }

        private bool IsPosWalkable(Vector2Int pos)
        {
            Cell cell = CellAt(pos);
            if (cell == null)
            {
                return false; // outside of map
            }

            if (cell.Type == CellType.Wall)
            {
                return false;
            }

            if (_tokens.Exists(x => x.Pos == pos))
            {
                return false; // token at position
            }

            return true;
        }

        private void MoveTokenView(Token clickedToken, Vector2Int tokenTarget, int movementLength)
        {
            GameObject clickedTokenView = _tokenViews[clickedToken];
            Vector3 tokenStart = clickedTokenView.transform.position;
            Vector3 tokenEnd = tokenTarget.WorldPos();

            // Rotate arrow
            Vector2Int facing = clickedToken.Facing;
            Transform arrowTransform = clickedTokenView.transform.Find("Arrow");
            Vector3 srcUp = arrowTransform.up;
            Vector3 targetUp = new Vector3(facing.x, facing.y, 0);
            Curve.Tween(AnimationCurve.EaseInOut(0, 0, 1, 1), 0.2f,
                t =>
                {
                    arrowTransform.up = Vector3.Lerp(srcUp, targetUp, t);
                },
                () =>
                {
                    arrowTransform.up = targetUp;
                });

            // Move token itself
            _isTokenMoving = true;
            CoroutineStarter.RunDelayed(_movementStartDelay, () =>
            {
                Curve.Tween(AnimationCurve.EaseInOut(0, 0, 1, 1), movementLength * _movementDurationPerCell,
                    t =>
                    {
                        clickedTokenView.transform.position = Vector3.Lerp(tokenStart, tokenEnd, t);
                    },
                    () =>
                    {
                        _isTokenMoving = false;
                        clickedTokenView.transform.position = tokenEnd;
                    });
            });
        }

        public void OnRestartClicked()
        {
            Sfx.Instance.Play("ClickButton");
            CoroutineStarter.Run(LoadLevel(_currentLevel));
        }

        public void OnNextButtonClicked()
        {
            Sfx.Instance.Play("ClickButton");
            CoroutineStarter.Run(LoadLevel(_currentLevel));
        }
    }
}