using System.Collections.Generic;
using UnityEngine;

public class HeatMap : MonoBehaviour
{
    private bool isDisabled = true;
    
    public bool showHeatMap;
    private MapGrid<HeatNode> _heatMap;

    private Sprite _whitePixelSprite;

    private List<GameObject> _pixels;
    float _cellSide = 0.25f;

    public void Initiate(Bounds bounds)
    {
        if(isDisabled) return;
        
        _heatMap = new MapGrid<HeatNode>(bounds, _cellSide, _cellSide);

        HeatNode[,] map = _heatMap.GetGrid();

        for (int i = 0; i < map.GetLength(0); i++)
        for (int j = 0; j < map.GetLength(1); j++)
            map[i, j].position = _heatMap.GetWorldPosition(i, j);

        _whitePixelSprite = Resources.Load<Sprite>("Sprites/white_pixel");
        _pixels = new List<GameObject>();
    }
    

    public void IncrementHeatMapVisibility(List<Guard> guards, float timeDelta)
    {
        if(isDisabled) return;
        
        HeatNode[,] map = _heatMap.GetGrid();

        for (int i = 0; i < map.GetLength(0); i++)
        for (int j = 0; j < map.GetLength(1); j++)
            CheckIfNodeVisible(guards, timeDelta, map[i, j]);
    }

    private void CheckIfNodeVisible(List<Guard> guards, float timeDelta, HeatNode node)
    {
        if(isDisabled) return;
        
        bool isVisible = false;

        foreach (var guard in guards)
        {
            isVisible = guard.GetFovPolygon().IsPointInPolygon(node.position, true);

            if (isVisible) break;
        }

        if (isVisible) node.Increment(timeDelta);
    }


    public void CalculateHeatValues()
    {
        if(isDisabled) return;
        
        float minValue = Mathf.Infinity;
        float maxValue = Mathf.NegativeInfinity;

        HeatNode[,] map = _heatMap.GetGrid();

        for (int i = 0; i < map.GetLength(0); i++)
        for (int j = 0; j < map.GetLength(1); j++)
        {
            HeatNode node = map[i, j];
            float spottedTime = node.GetTime();

            if (spottedTime < minValue) minValue = spottedTime;

            if (spottedTime > maxValue) maxValue = spottedTime;
        }


        for (int i = 0; i < map.GetLength(0); i++)
        for (int j = 0; j < map.GetLength(1); j++)
        {
            HeatNode node = map[i, j];
            float spottedTime = node.GetTime();

            node.heatValue = (spottedTime - minValue) / (maxValue - minValue);
        }
    }

    public void Clear()
    {
        if(isDisabled) return;
        
        while (_pixels.Count > 0)
        {
            GameObject pixel = _pixels[0];
            _pixels.RemoveAt(0);
            Destroy(pixel);
        }
    }


    public void RenderPixels()
    {
        if(isDisabled) return;
        
        HeatNode[,] map = _heatMap.GetGrid();

        for (int i = 0; i < map.GetLength(0); i++)
        for (int j = 0; j < map.GetLength(1); j++)
        {
            HeatNode node = map[i, j];
            
            if(!_heatMap.IsNodeInMap(node.position, _cellSide * 0.5f)) continue;
            
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

    public void OnDrawGizmos()
    {
        if (showHeatMap)
            _heatMap.Draw();
    }
}


public class HeatNode
{
    public Vector2 position;
    private float spottedTime;

    public float heatValue;

    public HeatNode()
    {
        spottedTime = 0f;
    }

    public void Increment(float timeDelta)
    {
        spottedTime += timeDelta;
    }

    public float GetTime()
    {
        return spottedTime;
    }

    public Color32 GetColor()
    {
        byte colorLevel = (byte) (heatValue * 255);
        Color32 color = new Color32(colorLevel, colorLevel, colorLevel, 255);

        return color;
    }
}