namespace Effects
{
    public class TimedImagePoolManager : PoolManager<TimedImage>
    {
        protected override bool IsActive(TimedImage poolItem)
        {
            return poolItem.gameObject.activeSelf;
        }

        protected override void Play(TimedImage poolItem)
        {
            poolItem.gameObject.SetActive(true);
        }
    }
}