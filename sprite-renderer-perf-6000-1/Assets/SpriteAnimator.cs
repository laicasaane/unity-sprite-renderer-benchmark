using System;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

public class SpriteAnimator : MonoBehaviour
{
    public int count = 1000;
    public int fps = 7;
    public Sprite[] sprites;
    public BoxCollider2D spawnRange;

    private SpriteRenderer[]      _rendererArray;
    private SpriteSheetInfo[]     _spriteSheetInfoArray;
    private SpriteIndex[]         _spriteIndexArray;
    private SpriteIndexPrevious[] _spriteIndexPreviousArray;
    private SpriteInterval[]      _spriteIntervalArray;
    private SpriteElapsedTime[]   _spriteElapsedTimeArray;
    private FaceDirection[]       _faceDirectionArray;

    static readonly ProfilerMarker s_animateMarker = new(nameof(AnimateSpriteSystem));
    static readonly ProfilerMarker s_syncMarker = new(nameof(SyncSpriteSystem));

    private void Awake()
    {
        var count = Mathf.Max(this.count, 1);

        var rendererArray = _rendererArray = new SpriteRenderer[count];
        var spriteSheetInfoArray = _spriteSheetInfoArray = new SpriteSheetInfo[count];
        var spriteIndexArray = _spriteIndexArray = new SpriteIndex[count];
        var spriteIndexPreviousArray = _spriteIndexPreviousArray = new SpriteIndexPrevious[count];
        var spriteIntervalArray = _spriteIntervalArray = new SpriteInterval[count];
        var spriteElapsedTimeArray = _spriteElapsedTimeArray = new SpriteElapsedTime[count];
        var faceDirectionArray = _faceDirectionArray = new FaceDirection[count];

        var sprites = this.sprites;
        var spriteSheetInfo = new SpriteSheetInfo { length = sprites.Length };
        var spriteIndexLast = sprites.Length - 1;
        var spriteInterval = 1f / math.max(fps, 1);

        var offset = spawnRange.transform.position;
        var size = spawnRange.size;
        var rangeMin = new float3(offset.x - size.x / 2, offset.y - size.y / 2, 0f);
        var rangeMax = new float3(offset.x + size.x / 2, offset.y + size.y / 2, 0f);
        var spawnRandom = Unity.Mathematics.Random.CreateFromIndex((uint)UnityEngine.Random.Range(0, 100));

        for (int i = 0; i < count; i++)
        {
            var spriteIndex = UnityEngine.Random.Range(0, spriteIndexLast);
            var faceDirection = UnityEngine.Random.Range(-1, 1);

            var go = new GameObject($"char-{i}");
            go.transform.position = spawnRandom.NextFloat3(rangeMin, rangeMax);

            rendererArray[i] = go.AddComponent<SpriteRenderer>();
            spriteSheetInfoArray[i] = spriteSheetInfo;
            spriteIndexArray[i] = new SpriteIndex { value = spriteIndex };
            spriteIndexPreviousArray[i] = new SpriteIndexPrevious { value = spriteIndex - 1 };
            spriteIntervalArray[i] = new SpriteInterval { value = spriteInterval };
            spriteElapsedTimeArray[i] = new SpriteElapsedTime { value = 0f };
            faceDirectionArray[i] = new FaceDirection { value = faceDirection };
        }
    }

    private void Update()
    {
        AnimateSpriteSystem();
        SyncSpriteSystem();
    }

    private void AnimateSpriteSystem()
    {
        s_animateMarker.Begin();

        var rendererArray = _rendererArray;
        var spriteSheetInfoArray = _spriteSheetInfoArray;
        var spriteIndexArray = _spriteIndexArray;
        var spriteIndexPreviousArray = _spriteIndexPreviousArray;
        var spriteIntervalArray = _spriteIntervalArray;
        var spriteElapsedTimeArray = _spriteElapsedTimeArray;

        var length = rendererArray.Length;
        var deltaTime = Time.smoothDeltaTime;

        for (var i = 0; i < length; i++)
        {
            ref var spriteSheetInfo = ref spriteSheetInfoArray[i];
            ref var spriteIndex = ref spriteIndexArray[i];
            ref var spriteIndexPrevious = ref spriteIndexPreviousArray[i];
            ref var spriteInterval = ref spriteIntervalArray[i];
            ref var spriteElapsedTime = ref spriteElapsedTimeArray[i];

            Animate(
                  ref spriteIndex
                , ref spriteIndexPrevious
                , ref spriteElapsedTime
                , in spriteInterval
                , in spriteSheetInfo
                , in deltaTime
            );
        }

        s_animateMarker.End();
    }

    private void SyncSpriteSystem()
    {
        s_syncMarker.Begin();

        var rendererArray = _rendererArray;
        var spriteSheetInfoArray = _spriteSheetInfoArray;
        var spriteIndexArray = _spriteIndexArray;
        var spriteIndexPreviousArray = _spriteIndexPreviousArray;
        var faceDirectionArray = _faceDirectionArray;
        var sprites = this.sprites;

        var length = rendererArray.Length;

        for (var i = 0; i < length; i++)
        {
            var renderer = rendererArray[i];
            ref var spriteSheetInfo = ref spriteSheetInfoArray[i];
            ref var spriteIndex = ref spriteIndexArray[i];
            ref var spriteIndexPrevious = ref spriteIndexPreviousArray[i];
            ref var faceDirection = ref faceDirectionArray[i];

            SyncSprite(
                  in spriteIndex
                , in spriteIndexPrevious
                , in spriteSheetInfo
                , in faceDirection
                , renderer
                , sprites
            );
        }

        s_syncMarker.End();
    }

    private static void Animate(
          ref SpriteIndex spriteIndex
        , ref SpriteIndexPrevious spriteIndexPrev
        , ref SpriteElapsedTime spriteElapsedTime
        , in SpriteInterval spriteInterval
        , in SpriteSheetInfo sheetInfo
        , in float deltaTime
    )
    {
        var elapsedTime = spriteElapsedTime.value;
        elapsedTime += deltaTime;

        var canUpdate = elapsedTime >= spriteInterval.value;
        var length = sheetInfo.length;
        var index = spriteIndex.value;
        var indexPrev = spriteIndexPrev.value;

        index = math.select(index, (index + 1) % length, canUpdate);
        indexPrev = math.select(indexPrev, (indexPrev + 1) % length, canUpdate);

        spriteElapsedTime.value = math.select(elapsedTime, 0f, canUpdate);
        spriteIndexPrev.value = indexPrev;
        spriteIndex.value = index;
    }

    private static void SyncSprite(
          in SpriteIndex spriteIndex
        , in SpriteIndexPrevious spriteIndexPrev
        , in SpriteSheetInfo spriteSheetInfo
        , in FaceDirection faceDirection
        , SpriteRenderer renderer
        , Sprite[] sprites
    )
    {
        var length = spriteSheetInfo.length;

        if (length < 1)
        {
            return;
        }

        if (spriteIndex.value == spriteIndexPrev.value)
        {
            return;
        }

        var index = spriteIndex.value % length;
        var sprite = sprites[index];

        renderer.sprite = sprite;
        renderer.flipX = faceDirection.GetFace() > 0;
    }
}

[Serializable]
public struct SpriteSheetInfo
{
    public int length;
}

[Serializable]
public struct SpriteIndex
{
    public int value;
}

[Serializable]
public struct SpriteIndexPrevious
{
    public int value;
}

[Serializable]
public struct SpriteInterval
{
    public float value;
}

[Serializable]
public struct SpriteElapsedTime
{
    public float value;
}

[Serializable]
public struct FaceDirection
{
    public int value;

    public readonly int GetFace()
        => math.select(-1, 1, value > 0);
}
