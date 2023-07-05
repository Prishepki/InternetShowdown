public interface IEverywhereCanvas
{
    public bool Active { get; set; }
    public void Reset();
    public void OnDisconnect();
}
