using UnityEngine;
#nullable enable

internal static class UnityHelper
{
    /// <summary>
    /// コンポーネントの取得を試みる。取得できなかった場合はエラーログを出力する。
    /// </summary>
    /// <typeparam name="T">取得するコンポーネントの型</typeparam>
    /// <param name="gameObject">コンポーネントを取得するゲームオブジェクト</param>
    /// <param name="result">取得したコンポーネントの格納先</param>
    public static bool TryGetComponentWithError<T>(this GameObject gameObject, out T result) where T : Component
    {
        if (gameObject.TryGetComponent(out result))
        {
            return true;
        }

        Debug.LogError(gameObject.name + " doesn't have " + typeof(T).Name);
        return false;
    }

    /// <summary>
    /// コンポーネントの取得を試みる。取得できなかった場合はエラーログを出力する。
    /// </summary>
    /// <typeparam name="T">取得するコンポーネントの型</typeparam>
    /// <param name="gameObject">コンポーネントを取得するゲームオブジェクト</param>
    /// <returns>取得したコンポーネント</returns>
    public static T GetComponentWithError<T>(this GameObject gameObject) where T : Component
    {
        if (gameObject.TryGetComponent(out T result))
        {
            return result;
        }

        Debug.LogError(gameObject.name + " doesn't have " + typeof(T).Name);
        return default!;
    }

    /// <summary>
    /// コンポーネントの取得を試みる。取得できなかった場合はエラーログを出力する。
    /// </summary>
    /// <typeparam name="T">取得するコンポーネントの型</typeparam>
    /// <param name="monoBehaviour">コンポーネントを取得するゲームオブジェクト</param>
    /// <param name="result">取得したコンポーネントの格納先</param>
    public static bool TryGetComponentWithError<T>(this MonoBehaviour monoBehaviour, out T result) where T : Component
    {
        if (monoBehaviour.TryGetComponent(out result))
        {
            return true;
        }

        Debug.LogError(monoBehaviour.name + " doesn't have " + typeof(T).Name);
        return false;
    }

    /// <summary>
    /// コンポーネントの取得を試みる。取得できなかった場合はエラーログを出力する。
    /// </summary>
    /// <typeparam name="T">取得するコンポーネントの型</typeparam>
    /// <param name="monoBehaviour">コンポーネントを取得するゲームオブジェクト</param>
    /// <returns>取得したコンポーネント</returns>
    public static T GetComponentWithError<T>(this MonoBehaviour monoBehaviour) where T : Component
    {
        if (monoBehaviour.TryGetComponent(out T result))
        {
            return result;
        }

        Debug.LogError(monoBehaviour.name + " doesn't have " + typeof(T).Name);
        return default!;
    }
}
