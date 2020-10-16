using LiveSplit.Model;
using LiveSplit.VoxSplitter;
using System.Reflection;

namespace LiveSplit.TheLastCampfire {
    public class TheLastCampfireComponent : Component {
        
        protected override EGameTime GameTime => EGameTime.Loading;
        
        public TheLastCampfireComponent(LiveSplitState state, Assembly assembly) : base(state, assembly) {
            memory = new TheLastCampfireMemory(logger);
            settings = new TreeSettings(assembly, Start, Reset, Options);
            settings.SetSettings(state.Run.AutoSplitterSettings);
        }
    }
}