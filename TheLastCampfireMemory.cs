using LiveSplit.ComponentUtil;
using LiveSplit.Model;
using LiveSplit.VoxSplitter;
using System;
using System.Collections.Generic;

namespace LiveSplit.TheLastCampfire {
    public class TheLastCampfireMemory : Memory {

        private Pointer<IntPtr> constants;
        private Pointer<int> gameMode;
        private Pointer<IntPtr> chests;
        private Pointer<bool> faded;
        private Pointer<IntPtr> modeSelect;
        private Pointer<IntPtr> events;
        private StringPointer puzzle;

        private int chestsCount = 0;
        private int eventsCount = 0;

        private bool isExploration = false;
        private bool canEnd = false;

        private readonly RemainingDictionary remainingSplits;

        private readonly MonoHelper mono;
        
        public TheLastCampfireMemory(Logger logger) : base(logger) {
            processName = "The Last Campfire";
            remainingSplits = new RemainingDictionary(logger);
            mono = new MonoHelper(this);
        }

        public override bool IsReady() => base.IsReady() && mono.IsCompleted;

        protected override void OnGameHook() {
            mono.Run(() => {
                MonoNestedPointerFactory ptrFactory = new MonoNestedPointerFactory(this, mono);
                
                constants = ptrFactory.Make<IntPtr>("GameConstants", "_instance", out var constClass);
                gameMode = ptrFactory.Make<int>(constants, mono.GetFieldOffset(constClass, "currentGameMode"));

                chests = ptrFactory.Make<IntPtr>("CollectableManager", "_instance", "m_chestsCollected");
                
                Pointer<IntPtr> guiInstance = ptrFactory.Make<IntPtr>("TBFGUIManager", "_instance", out var guiClass);
                faded = ptrFactory.Make<bool>(guiInstance, mono.GetFieldOffset(guiClass, "_faded"));
                modeSelect = ptrFactory.Make<IntPtr>(guiInstance, mono.GetFieldOffset(guiClass, "pages"), 0x18, 0x60, 0x100);

                events = ptrFactory.Make<IntPtr>("EventManager", "_instance", "allValues");

                puzzle = ptrFactory.MakeString("PuzzleManager", "s_instance", "m_lastCompletedPuzzleName", 0x14);
                puzzle.StringType = EStringType.UTF16Sized;

                logger.Log(ptrFactory.ToString());
            });
        }

        public override bool Start(int start) => modeSelect.Old != default && modeSelect.New == default;

        public override void OnStart(TimerModel timer, HashSet<string> splits) {
            remainingSplits.Setup(splits);

            isExploration = constants != null && game.Read<bool>(constants.New + 0x68);
            canEnd = false;
        }

        public override bool Split() {
            return remainingSplits.Count != 0 && (SplitEventsAndPuzzles() || SplitChests() || SplitEnd());

            bool SplitEventsAndPuzzles() {
                if(!remainingSplits.ContainsKey("Event")) { return false; }

                int count = eventsCount;
                eventsCount = game.Read<int>(events.New + 0x40);
                if(count < eventsCount) {
                    IntPtr eventArr = game.Read<IntPtr>(events.New + 0x18);
                    for(; count < eventsCount; count++) {
                        IntPtr eventPtr = game.Read<IntPtr>(eventArr + 0x28 + 0x18 * count);
                        string eventName = game.ReadString(eventPtr + 0x14, EStringType.UTF16Sized);
                        Console.WriteLine(eventName);
                        if(eventName.StartsWith("PUZZLE_")) {
                            if(isExploration) {
                                if(remainingSplits.Split("Event", eventName.Substring(7))) {
                                    return true;
                                }
                            } else {
                                continue;
                            }
                        } else if(eventName.Equals("REDEMBER_PUZZLE")) {
                            canEnd = true;
                        }
                        if(remainingSplits.Split("Event", eventName)) {
                            return true;
                        }
                    }
                }

                return !isExploration && puzzle.Changed && remainingSplits.Split("Event", puzzle.New);
            }

            bool SplitChests() {
                if(!remainingSplits.ContainsKey("Chest")) { return false; }

                int count = chestsCount;
                chestsCount = game.Read<int>(chests.New + 0x18);
                if(count < chestsCount) {
                    IntPtr chestArr = game.Read<IntPtr>(chests.New + 0x10);
                    for(; count < chestsCount; count++) {
                        IntPtr chestPtr = game.Read<IntPtr>(chestArr + 0x20 + 0x8 * count);
                        string chestName = game.ReadString(chestPtr + 0x14, EStringType.UTF16Sized);
                        if(remainingSplits.Split("Chest", chestName)) {
                            return true;
                        }
                    }
                }

                return false;
            }

            bool SplitEnd() {
                return remainingSplits.ContainsKey("End") && canEnd && !faded.Old && faded.New && remainingSplits.Split("End");
            }
        }

        public override bool Reset(int reset) => gameMode.Old != 1 && gameMode.New == 1;

        public override bool Loading() => gameMode.New == 0;

        public override void Dispose() {
            base.Dispose();
            mono.Dispose();
        }
    }
}