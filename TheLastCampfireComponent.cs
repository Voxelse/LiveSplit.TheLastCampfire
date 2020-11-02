using LiveSplit.Model;
using LiveSplit.VoxSplitter;

namespace LiveSplit.TheLastCampfire {
    public class TheLastCampfireComponent : Component {
        
        protected override EGameTime GameTime => EGameTime.Loading;
        
        public TheLastCampfireComponent(LiveSplitState state) : base(state) {
            memory = new TheLastCampfireMemory(logger);
            settings = new TreeSettings(state, Start, Reset, Options);
        }
    }
}