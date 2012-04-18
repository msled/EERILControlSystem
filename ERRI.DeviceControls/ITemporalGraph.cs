namespace EERIL.DeviceControls {
    public interface ITemporalGraph<T> {
        void AppendState(T value, long timestamp);
        void Range(long start, long end);
    }

    public class TemporalGraph<T> : ITemporalGraph<T> {
        public void AppendState(T value, long timestamp) {
            throw new System.NotImplementedException();
        }

        public void Range(long start, long end) {
            throw new System.NotImplementedException();
        }
    }
}
