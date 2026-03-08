namespace BringTheBrotliDemo
{
    public struct MinigameResult
    {
        public ResourceType ResourceType;
        public int ResourceDelta;

        public MinigameResult(ResourceType type, int delta)
        {
            ResourceType = type;
            ResourceDelta = delta;
        }
    }
}
