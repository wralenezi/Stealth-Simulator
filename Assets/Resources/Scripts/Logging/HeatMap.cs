using System.Collections.Generic;
using UnityEngine;

public class HeatMap : MonoBehaviour
{
    // private bool isDisabled = true;

    private bool isRenderMap = false;

    public bool showHeatMap;
    private MapGrid<HeatNode> _heatMap;

    private List<HeatNode> _heatNodes;

    private Sprite _whitePixelSprite;

    private List<GameObject> _pixels;
    float _cellSide = 0.5f;

    public void Initiate(Bounds bounds)
    {
        _heatMap = new MapGrid<HeatNode>(bounds, _cellSide, _cellSide);
        _heatNodes = new List<HeatNode>();

        _whitePixelSprite = Resources.Load<Sprite>("Sprites/white_pixel");
        _pixels = new List<GameObject>();
    }

    public void IncrementHeatMapVisibility(List<Guard> guards, float timeDelta)
    {
        foreach (var node in _heatNodes)
            CheckIfNodeVisible(guards, timeDelta, node);
    }

    private void CheckIfNodeVisible(List<Guard> guards, float timeDelta, HeatNode node)
    {
        bool isVisible = false;

        foreach (var guard in guards)
        {
            isVisible = guard.GetFovPolygon().IsPointInPolygon(node.position, true);

            if (isVisible) break;
        }

        if (isVisible) node.Increment(timeDelta);
    }


    // Set the normalized heat values
    private void CalculateHeatValues()
    {
        // if (isDisabled) return;

        float minValue = Mathf.Infinity;
        float maxValue = Mathf.NegativeInfinity;

        foreach (var node in _heatNodes)
        {
            float spottedTime = node.GetTime();

            if (spottedTime < minValue) minValue = spottedTime;

            if (spottedTime > maxValue) maxValue = spottedTime;
        }

        foreach (var node in _heatNodes)
        {
            float spottedTime = node.GetTime();
            node.heatValue = (spottedTime - minValue) / (maxValue - minValue);
        }
    }
    
    

    public void Reset()
    {
        foreach (var node in _heatNodes)
            node.Reset();

        _heatNodes.Clear();

        HeatNode[,] map = _heatMap.GetGrid();

        for (int i = 0; i < map.GetLength(0); i++)
        for (int j = 0; j < map.GetLength(1); j++)
        {
            map[i, j].position = _heatMap.GetWorldPosition(i, j);
            map[i, j].Col = i;
            map[i, j].Row = j;

            if (_heatMap.IsNodeInMap(map[i, j].position, _cellSide)) _heatNodes.Add(map[i, j]);
        }

        while (_pixels.Count > 0)
        {
            GameObject pixel = _pixels[0];
            _pixels.RemoveAt(0);
            Destroy(pixel);
        }
    }


    private void RenderPixels()
    {
        if (!isRenderMap) return;

        foreach (var node in _heatNodes)
        {
            GameObject pixel = new GameObject();
            pixel.transform.parent = transform;
            pixel.transform.position = node.position;

            SpriteRenderer spriteRenderer = pixel.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = _whitePixelSprite;
            float newScale = _cellSide * 100f;
            spriteRenderer.transform.localScale = new Vector2(newScale, newScale);

            spriteRenderer.color = node.GetColor();

            _pixels.Add(spriteRenderer.gameObject);
        }
    }


    public string GetHeatMapResult(bool isFileExist)
    {
        // Write the exploration results for this episode
        string data = "";

        if (!isFileExist) data += "Col,Row,Time,EpisodeTime,heatValue,EpisodeID" + "\n";

        foreach (var node in _heatNodes)
        {
            data += node.Col + "," + node.Row + "," + node.GetTime() + "," + StealthArea.SessionInfo.episodeLengthSec +
                    "," +
                    node.heatValue + "," +
                    StealthArea.SessionInfo.currentEpisode + "\n";
        }

        return data;
    }

    private void WriteResults()
    {
        if (!Equals(GameManager.Instance.loggingMethod, Logging.None))
            CsvController.WriteString(
                CsvController.GetPath(StealthArea.SessionInfo, FileType.HeatMap, null),
                GetHeatMapResult(CsvController.IsFileExist(StealthArea.SessionInfo, FileType.HeatMap, null)), true);
    }


    public void End()
    {
        // if (isDisabled) return;

        CalculateHeatValues();
        RenderPixels();
        WriteResults();
        foreach (var node in _heatNodes)
            node.Reset();
        _heatNodes.Clear();
    }


    public void OnDrawGizmos()
    {
        if (showHeatMap) _heatMap.Draw();
    }
}