﻿using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using NHunspell;

namespace SpellTextBox
{
    public class SpellTextBox : TextBox
    {
        static SpellTextBox()
        {
            TextProperty.OverrideMetadata(typeof(SpellTextBox), new FrameworkPropertyMetadata(new PropertyChangedCallback(TextPropertyChanged)));
        }

        public SpellTextBox() : base()
        {
            CreateTimer();

            var cm = new ContextMenu();
            this.ContextMenu = cm;
            this.ContextMenu.Opened += OnContextMenuOpening;

            Loaded += (s, e) =>
            {
                Initialize();
                if (Window.GetWindow(this) != null)
                    Window.GetWindow(this).Closing += (s1, e1) => Dispose();
            };
        }

        #region Timer

        static Timer textChangedTimer = new System.Timers.Timer(500);
        ElapsedEventHandler textChangedTimerOnElapse;

        void CreateTimer()
        {
            textChangedTimer.AutoReset = false;
            textChangedTimerOnElapse = new ElapsedEventHandler(textChangedTimer_Elapsed);
            textChangedTimer.Elapsed += textChangedTimerOnElapse;

        }

        private static void TextPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            (sender as SpellTextBox).RunSplellChecking();
        }

        public void RunSplellChecking()
        {
            IsSpellcheckCompleted = false;
            textChangedTimer.Stop();
            textChangedTimer.Start();
        }

        private void textChangedTimer_Elapsed(object sender,
        System.Timers.ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new System.Action(() =>
            {
                Checker.CheckSpelling(Text);
                RaiseSpellcheckCompletedEvent();
            }));
        }

        #endregion

        #region SpellcheckCompleted Event

        public static readonly RoutedEvent SpellcheckCompletedEvent = EventManager.RegisterRoutedEvent(
            nameof(SpellcheckCompleted), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SpellTextBox));

        public event RoutedEventHandler SpellcheckCompleted
        {
            add { AddHandler(SpellcheckCompletedEvent, value); }
            remove { RemoveHandler(SpellcheckCompletedEvent, value); }
        }

        void RaiseSpellcheckCompletedEvent()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(SpellTextBox.SpellcheckCompletedEvent);
            RaiseEvent(newEventArgs);
            IsSpellcheckCompleted = true;
        }

        public bool IsSpellcheckCompleted { get; set; }

        #endregion

        protected void OnContextMenuOpening(object sender, RoutedEventArgs e)
        {
            if (Checker.MisspelledWords.Any(w => SelectionStart >= w.Index && SelectionStart <= w.Index + w.Length))
                Checker.SelectedMisspelledWord = Checker.MisspelledWords.First(w => SelectionStart >= w.Index && SelectionStart <= w.Index + w.Length);
            else
                Checker.SelectedMisspelledWord = null;

            this.ContextMenu.Items.Clear();
            foreach (var item in Checker.MenuActions)
            {
                var mi = new MenuItem();
                mi.Header = item.Name;
                mi.Command = item.Command;
                this.ContextMenu.Items.Add(mi);
            }
        }

        public static readonly DependencyProperty DictionaryPathProperty =
            DependencyProperty.Register(
            nameof(DictionaryPath),
            typeof(string),
            typeof(SpellTextBox)
            , new FrameworkPropertyMetadata(DictionaryPathChanged)
        );

        private static void DictionaryPathChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            (sender as SpellTextBox).UpdateChecker();
        }

        public string DictionaryPath
        {
            get { return (string)this.GetValue(DictionaryPathProperty); }
            set { this.SetValue(DictionaryPathProperty, value); }
        }

        public static readonly DependencyProperty CustomDictionaryPathProperty =
            DependencyProperty.Register(
            nameof(CustomDictionaryPath),
            typeof(string),
            typeof(SpellTextBox)
        );

        public string CustomDictionaryPath
        {
            get { return (string)this.GetValue(CustomDictionaryPathProperty) ?? "custom.txt"; }
            set { this.SetValue(CustomDictionaryPathProperty, value); }
        }

        public static readonly DependencyProperty IsSpellCheckEnabledProperty =
            DependencyProperty.Register(
            nameof(IsSpellCheckEnabled),
            typeof(bool),
            typeof(SpellTextBox)
        );

        public bool IsSpellCheckEnabled
        {
            get { return (bool)this.GetValue(IsSpellCheckEnabledProperty); }
            set { this.SetValue(IsSpellCheckEnabledProperty, value); }
        }

        AdornerLayer myAdornerLayer;
        RedUnderlineAdorner myAdorner;

        public void Initialize()
        {
            myAdornerLayer = AdornerLayer.GetAdornerLayer(this);
            myAdorner = new RedUnderlineAdorner(this);
            myAdornerLayer.Add(myAdorner);
        }

        public void Dispose()
        {
            myAdorner.Dispose();
            myAdornerLayer.Remove(myAdorner);
            this.ContextMenu.Opened -= OnContextMenuOpening;
            textChangedTimer.Elapsed -= textChangedTimerOnElapse;
            if (checker != null)
                checker.Dispose();
        }

        private SpellChecker checker;

        public SpellChecker Checker
        {
            get { return checker ?? CreateSpellCheker(); }
            set
            {
                if (checker != value)
                {
                    checker?.Dispose();
                    checker = value;
                    RunSplellChecking();
                }
            }
        }

        private SpellChecker CreateSpellCheker()
        {
            var checker = new SpellChecker(new Hunspell(DictionaryPath + ".aff", DictionaryPath + ".dic"), this);
            checker.LoadCustomDictionary();
            return checker;
        }

        private void UpdateChecker()
        {
            Checker = CreateSpellCheker();
        }

        public void ReplaceSelectedWord(Word WordToReplaceWith)
        {
            if (WordToReplaceWith.Text != StringResources.NoSuggestions)
            {
                int index = Checker.SelectedMisspelledWord.Index;
                string replacement = WordToReplaceWith.Text;
                SetCurrentValue(TextProperty, Text.Remove(index, Checker.SelectedMisspelledWord.Length).Insert(index, replacement));
                SelectionStart = index + WordToReplaceWith.Length;
            }
        }

        public void FireTextChangeEvent()
        {
            int c = SelectionStart;
            string s = Text;
            SetCurrentValue(TextProperty, s + " ");
            SetCurrentValue(TextProperty, s);
            SelectionStart = c;
        }

        private ICommand _replace;
        public ICommand Replace
        {
            get
            {
                return _replace ?? (_replace = new DelegateCommand(
                        delegate
                        {
                            ReplaceSelectedWord(checker.SelectedSuggestedWord);
                        }));
            }
        }
    }
}
