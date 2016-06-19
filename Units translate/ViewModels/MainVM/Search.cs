using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core;

namespace Units_translate
{
    public partial class MainVM
    {
        const string CASE_INSANSITIVE_SEARCH = "CASE_INSANSITIVE_SEARCH";
        /// <summary>
        /// Делает поиск не чуствительным к регистру символов
        /// </summary>
        public bool CaseInsensitiveSearch
        {
            get { return Core.Search.CaseInsensitiveSearch; }
            set
            {
                if (Core.Search.CaseInsensitiveSearch != value)
                {
                    Core.Search.CaseInsensitiveSearch = value;
                    Helpers.ConfigWrite(CASE_INSANSITIVE_SEARCH, Core.Search.CaseInsensitiveSearch);
                    NotifyPropertyChanged(nameof(CaseInsensitiveSearch));
                }
            }
        }

        /// <summary>
        /// Вызывает поиск строки в словаре используя заданный текст как регулярку
        /// </summary>
        public string SearchText
        {
            set { Search(value); }
        }

        bool _Searching = false;
        public bool Searching
        {
            get { return _Searching; }
            protected set
            {
                _Searching = value;
                NotifyPropertyChanged(nameof(Searching));
            }
        }

        CancellationTokenSource _SearchCToken = new CancellationTokenSource();
        void Search(string Expr)
        {
            _SearchCToken.Cancel();
            var localToken = _SearchCToken = new CancellationTokenSource();
            Searching = true;
            Task.Factory.StartNew(() =>
            {
                var res = Core.Search.Exec(Expr, localToken.Token);
                Helpers.mainCTX.Send(_ =>
                {
                    SearchResults = res?.OfType<IMapRecord>().ToArray();
                    NotifyPropertyChanged(nameof(SearchResults));
                    if (_SearchCToken == localToken)
                        Searching = false;
                }, null);
            });
        }

        /// <summary>
        /// Результаты поиска
        /// </summary>
        public ICollection<IMapRecord> SearchResults { get; private set; }
    }
}
