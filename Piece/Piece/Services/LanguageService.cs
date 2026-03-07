namespace Piece.Services
{
	public class LanguageService
	{
		private string _currentLanguage = "en";

		public string CurrentLanguage => _currentLanguage;
		public bool IsBulgarian => _currentLanguage == "bg";

		public event Action? OnLanguageChanged;

		public void SetLanguage(string lang)
		{
			_currentLanguage = lang;
			OnLanguageChanged?.Invoke();
		}

		public string T(string key) =>
			Translations.TryGetValue((_currentLanguage, key), out var val) ? val : key;

		private static readonly Dictionary<(string lang, string key), string> Translations = new()
		{
            // Nav
            { ("bg", "DISCOVER") , "ОТКРИЙ" },
            { ("bg", "YOURS") , "ТВОИ" },
            { ("bg", "ACCOUNT") , "АКАУНТ" },
            { ("bg", "ADMIN") , "АДМИН" },
			{ ("bg", "Player") , "Плейър" },
			{ ("bg", "Search") , "Търси" },
			{ ("bg", "Map") , "Карта" },
			{ ("bg", "Visualizer") , "Визуализатор" },
			{ ("bg", "Library") , "Библиотека" },
			{ ("bg", "Favorites") , "Любими" },
			{ ("bg", "Profile") , "Профил" },
			{ ("bg", "Settings") , "Настройки" },
			{ ("bg", "Logout") , "Изход" },
			{ ("bg", "Subscriptions") , "Абонаменти" },
			{ ("bg", "Dashboard") , "Контролен панел" },
			{ ("bg", "Upload Music") , "Качи музика" },
			{ ("bg", "Weekly Colors") , "Седмични цветове" },


			//Search page
			{ ("bg", "Search users") , "Tърси потребители" },
			{ ("bg", "Searching...") , "Tърсят се..." },
			{ ("bg", "That one person...") , "Този един човек..." },
			{ ("bg", "Find Piece users") , "Намери потребители на Piece" },
			{ ("bg", "Search for users by their username or display name") , "Търси за потребители по тяхното потребителско име или прякор" },
			{ ("bg", "No users found") , "Не са намерени потребители" },
			{ ("bg", "Try a different search term") , "Опитай различна фраза" },
			{ ("bg", "Foиnd") , "Намерени са" },
			{ ("bg", "Found") , "Намерен е" },
			{ ("bg", "user") , "потребител" },
			{ ("bg", "users") , "потребители" },
			{ ("bg", "Piece User") , "Потребител на Piece" },

            // Player
            { ("bg", "Loading music...") , "Зарежда се музиката..." },
            { ("bg", "Search the library or Jamendo...") , "Търси из библиотеката или из Jamendo..." },
			{ ("bg", "Filter by Genre:") , "Филтър по жанр:" },
			{ ("bg", "Filter by Jamendo Tag:") , "Филтър по Jamendo таг:" },
			{ ("bg", "All Genres") , "Всички жанрове" },
			{ ("bg", "Search by Artist:") , "Търси по изпълнител" },
			{ ("bg", "Year:") , "Година:" },
			{ ("bg", "Clear Filters") , "Изчисти филтрите" },
			{ ("bg", "No tracks found") , "Не са намерени парчета" },
			{ ("bg", "Library is empty") , "Библиотеката е празна" },
			{ ("bg", "genre:") , "жанр:" },
			{ ("bg", "artist:") , "изпълнител:" },
			{ ("bg", "No tracks found for genre:") , "Не са намерени парчета от жанра:" },
			{ ("bg", "Recommended tracks") , "Препоръчани парчета" },
			{ ("bg", "Artist name...") , "Име..." },
			{ ("bg", "No Jamendo results found") , "Не са намерени резултати от Jamendo" },
			{ ("bg", "e.g., 2020") , "пример: 2020" },

            
			//Queue
			{ ("bg", "No tracks in queue") , "Няма парчета в опашката" },
			{ ("bg", "Clear Queue") , "Изчисти опашката" },
			{ ("bg", "track") , "парче" },
			{ ("bg", "tracks") , "парчета" },
			{ ("bg", "in queue") , "в опашката" },


			//Visualizer
			{ ("bg", "Play a song to start the visualizer") , "Пуснете някое парче за да стартирате визуализатора" },
			{ ("bg", "Back") , "Обратно" },
			{ ("bg", "Liquid Bars") , "Флуиди" },
			{ ("bg", "Circle") , "Кръг" },
			{ ("bg", "Wave") , "Вълна" },
			{ ("bg", "Particles") , "Частици" },
			{ ("bg", "DNA") , "ДНК" },
			{ ("bg", "3D Sphere") , "3D Сфера" },

			//Map
			{ ("bg", "Loading world map") , "Зарежда се световната карта" },
			{ ("bg", "Fetching data from MusicBrainz and Deezer APIs.") , "Извличане на данни от MusicBrains и Deezer APIs." },
			{ ("bg", "This may take a few moments.") , "Това може да отнеме малко време." },
			{ ("bg", "Big аrtists from") , "Известни изпълнители от" },
			{ ("bg", "No preview tracks available") , "Няма налични записи за преглед" },
			{ ("bg", "No artist data available for this country.") , "Няма намерени изпълнители от тази държава." },
			{ ("bg", "30-second preview") , "30-секунден преглед" },
			{ ("bg", "Loading artists and tracks") , "Зареждат се изпълнители и парчета" },



            // Playlists
            { ("bg", "Playlists") , "Плейлисти" },
			{ ("bg", "Create Playlist") , "Създай плейлист" },
			{ ("bg", "Edit Playlist") , "Преправи плейлист" },
			{ ("bg", "Loading playlists") , "Зареждат се плейлистите" },
			{ ("bg", "No playlists yet") , "Все още няма плейлисти" },
			{ ("bg", "Create your first playlist to organize your music") , "Създайте своя пръв плейлист за да организирате музиката си" },
			{ ("bg", "Edit") , "Промени" },
			{ ("bg", "Delete") , "Изтрий" },
			{ ("bg", "Name") , "Име" },
			{ ("bg", "Description (Optional)") , "Описание (по желание)" },
			{ ("bg", "Make this playlist public") , "Направи този плейлист публичен" },
			{ ("bg", "Cancel") , "Откажи" },
			{ ("bg", "My Awesome Playlist") , "Моят готин плейлист" },
			{ ("bg", "A collection of my favorite tracks") , "Колекция от любимите ми парчета" },
			{ ("bg", "Create") , "Създай" },
			{ ("bg", "Save Changes") , "Запази промените" },


			// Playlist Detail
			{ ("bg", "Back to Playlists"), "Обратно към плейлистите" },
			{ ("bg", "Loading playlist..."), "Зареждане на плейлиста..." },
			{ ("bg", "Playlist not found"), "Плейлистът не е намерен" },
			{ ("bg", "This playlist may have been deleted"), "Този плейлист може да е изтрит" },
			{ ("bg", "Playlist"), "Плейлист" },
			{ ("bg", "Admin View - This playlist belongs to another user"), "Администраторски изглед - Този плейлист принадлежи на друг	потребител" },
			{ ("bg", "Play"), "Пусни" },
			{ ("bg", "Shuffle"), "Разбъркай" },
			{ ("bg", "Shuffle On"), "Разбъркването е включено" },
			{ ("bg", "Shuffle Off"), "Разбъркването е изключено" },
			{ ("bg", "Tracks"), "Песни" },
			{ ("bg", "Add Tracks"), "Добави песни" },
			{ ("bg", "Unknown"), "Непознат" },
			{ ("bg", "Remove from playlist"), "Премахни от плейлиста" },
			{ ("bg", "No tracks yet"), "Няма песни все още" },
			{ ("bg", "Add some tracks to get started"), "Добави песни за да започнеш" },
			{ ("bg", "Add Tracks to Playlist"), "Добави песни към плейлиста" },
			{ ("bg", "Admin View Only"), "Само администраторски изглед" },
			{ ("bg", "You're viewing another user's playlist as an administrator."), "Разглеждаш плейлиста на друг потребител като администратор." },
			{ ("bg", "You cannot modify playlists that belong to other users."), "Не можеш да променяш плейлисти на други потребители." },
			{ ("bg", "Search your library..."), "Търси в библиотеката..." },
			{ ("bg", "Change Cover"), "Смени корицата" },
			{ ("bg", "Added"), "Добавено" },
			
            
			//LikedSongs
			{ ("bg", "Liked Songs"), "Любими песни" },
			{ ("bg", "songs"), "песни" },
			{ ("bg", "Mix of local and Jamendo tracks"), "Микс от локални и Jamendo песни" },
			{ ("bg", "Loading your liked songs..."), "Зареждане на харесаните песни..." },
			{ ("bg", "All"), "Всички" },
			{ ("bg", "Local"), "Локални" },
			{ ("bg", "No liked songs yet"), "Няма харесани песни все още" },
			{ ("bg", "Songs you like will appear here"), "Песните които харесаш ще се появят тук" },

			// Profile
			{ ("bg", "Loading profile..."), "Зареждане на профила..." },
			{ ("bg", "User Not Found"), "Потребителят не е намерен" },
			{ ("bg", "This user doesn't exist or their profile is private."), "Този потребител не съществува или профилът му е частен." },
			{ ("bg", "Private Profile"), "Частен профил" },
			{ ("bg", "This user's profile is set to private."), "Профилът на този потребител е зададен като частен." },
			{ ("bg", "Genres"), "Жанрове" },
			{ ("bg", "Edit Profile"), "Редактирай профила" },
			{ ("bg", "Top Genres"), "Топ жанрове" },
			{ ("bg", "Public Playlists"), "Публични плейлисти" },
			{ ("bg", "No public playlists yet"), "Няма публични плейлисти все още" },
			{ ("bg", "Create a playlist and make it public to share with others"), "Създай плейлист и го направи публичен за да го споделиш с	другите" },
			// Profile Edit Modal
			{ ("bg", "Change Avatar"), "Смени аватара" },
			{ ("bg", "Remove Avatar"), "Премахни аватара" },
			{ ("bg", "Display Name"), "Показвано име" },
			{ ("bg", "How you want to be called"), "Как искаш да те наричат" },
			{ ("bg", "Bio"), "Биография" },
			{ ("bg", "Tell us about your music taste..."), "Разкажи ни за музикалния си вкус..." },
			{ ("bg", "characters"), "символа" },
			{ ("bg", "Privacy Settings"), "Настройки за поверителност" },
			{ ("bg", "Public Profile - Anyone can view your profile"), "Публичен профил - Всеки може да види профила ти" },
			{ ("bg", "Show Public Playlists - Display your playlists on your profile"), "Покажи публичните плейлисти - Показва плейлистите ти в		профила" },
			{ ("bg", "Show Listening Colors - Display your top genres and listening stats"), "Покажи цветовете на слушане - Показва топ жанровете и				статистиките ти" },
			{ ("bg", "Saving..."), "Запазване..." },
			


			//Statistics
			{ ("bg", "Statistics"), "Статистика" },
			{ ("bg", "Loading statistics..."), "Зарежда се статистиката..." },
			{ ("bg", "Listening History (Last 90 Days)"), "История на слушаните парчета (за последните 90 дни)" },
			{ ("bg", "Genre Colors:"), "Цветове на жанровете" },
			{ ("bg", "Top Genres (Last 30 Days)"), "Най-слушани жанрове (за последните 30 дни)" },
			{ ("bg", "Most Played Tracks (All Time)"), "Най-слушани парчета досега" },
			{ ("bg", "No play history yet"), "Все още няма история на слушанията" },
			{ ("bg", "Recently Played"), "Най-скоро слушани" },
			{ ("bg", "No recent plays"), "Няма слушани наскоро" },


			// Subscriptions
			{ ("bg", "Choose Your Plan"), "Избери своя план" },
			{ ("bg", "Experience Piece at its fullest with Premium!"), "Преживей Piece в пълния му потенциал с Премиум!" },
			{ ("bg", "Without cost"), "Безплатно" },
			{ ("bg", "/month"), "/месец" },
			{ ("bg", "Interactive Map Feature"), "Интерактивна карта" },
			{ ("bg", "Current Plan"), "Текущ план" },
			{ ("bg", "Downgrade"), "Намали плана" },
			{ ("bg", "Upgrade"), "Надгради" },
			// Checkout modal
			{ ("bg", "Complete Your Purchase"), "Завърши покупката" },
			{ ("bg", "Order Summary"), "Обобщение на поръчката" },
			{ ("bg", "Plan"), "План" },
			{ ("bg", "Total"), "Общо" },
			{ ("bg", "Payment Information"), "Информация за плащане" },
			{ ("bg", "Card Number"), "Номер на карта" },
			{ ("bg", "Expiry Date"), "Дата на изтичане" },
			{ ("bg", "CVV"), "CVV" },
			{ ("bg", "Payment successful! Redirecting..."), "Плащането е успешно! Пренасочване..." },
			{ ("bg", "Processing..."), "Обработване..." },
			// Welcome modal
			{ ("bg", "Welcome to Premium!"), "Добре дошъл в Премиум!" },
			{ ("bg", "You now have access to exclusive features"), "Вече имаш достъп до ексклузивни функции" },
			{ ("bg", "Explore the World Map"), "Разгледай световната карта" },
			{ ("bg", "Discover music from every corner of the globe. Click on countries to explore local artists and genres."), "Открий музика от всеки			ъгъл на		света. Кликни върху държави за да разгледаш местни артисти и жанрове." },
			{ ("bg", "Explore Map Now"), "Разгледай картата" },
			{ ("bg", "Maybe Later"), "Може би по-късно" },


			//Home
			{ ("bg", "Welcome Back") , "Добре дошъл" },
			{ ("bg", "Where do you want to start?") , "От къде искаш да започнеш?" },
			{ ("bg", "Surprise Me") , "Изненадай ме" },
			{ ("bg", "Recently Played") , "Наскоро слушани" },
			{ ("bg", "View All") , "Виж всички" },
			{ ("bg", ">No listening history yet") , "Все още няма история на слушани парчета" },
			{ ("bg", "Start playing music to build your personal history") , "Пусни музика за да създадеш своята история на слушани парчета" },
			{ ("bg", "Browse Music") , "Търси музика" },
			{ ("bg", "Your Top Genre") , "Твоят най-слушан жанр" },
			{ ("bg", "plays this month") , "слушания този месец" },
			{ ("bg", "Quick Actions") , "Бързи действия" },
			{ ("bg", "Your Listening Journey") , "Твоето музикално пътуване" },
			{ ("bg", "Last 30 days — each color represents a genre") , "Последните 30 дни - всеки цвят показва жанр" },
			{ ("bg", "Full Stats") , "Пълна статистика" },


            // Landing
            { ("bg", "Log In") , "Влез" },
			{ ("bg", "Sign Up Free") , "Регистрирай се" },
			{ ("bg", "Music From Every Corner of the World") , "Музика от всяка точка на света" },
			{ ("bg", "500,000+ tracks. Real-time visualizers. Global discovery.") , "500,000+ парчета. Визуализатори в реално време. Глобално откритие." },
			{ ("bg", "Total Tracks") , "Общо парчета" },
			{ ("bg", "Plays Today") , "Слушания днес" },
			{ ("bg", "Continents") , "Континента" },
			{ ("bg", "Start Your Journey") , "Започни своето пътуване" },
			{ ("bg", "VISUALIZER") , "ВИЗУАЛИЗАТОР" },
			{ ("bg", "Feel the music,") , "Усети музиката," },
			{ ("bg", "not just hear it") , "вместо само да я слушаш" },
			{ ("bg", "Watch your music come alive with real-time audio visualizations. Five unique visual modes transform sound frequencies into stunning visual experiences — from pulsing bars to a breathing 3D sphere.") , "Гледай как музиката ти оживява с визуализациите в реално време. 5 уникални режима превръщат музикалните честоти в невероятно визуално преживяване - от пулсиращи флуиди до дишаща 3D сфера." },
			{ ("bg", "WORLD MAP") , "СВЕТОВНА КАРТА" },
			{ ("bg", "PREMIUM") , "ПРЕМИУМ" },
			{ ("bg", "Music has") , "Музиката" },
			{ ("bg", "no borders") , "няма граници" },
			{ ("bg", "Click on any country and instantly discover its most iconic artists, local genres, and 30-second track previews. From K-Pop in South Korea to Bossa Nova in Brazil — the world's music is at your fingertips.") , "Натисни на която и да е държава и веднага ще откриеш най-известните артисти от там и можеш да чуеш 30-секундни откъси от парчетата им. От К-Поп в Южна Корея до Боса Нова в Бразилия - световната музика е в твоите ръце." },
			{ ("bg", "Half a million tracks,") , "Половин милион парчета" },
			{ ("bg", "zero restrictions") , "0 ограничения" },
			{ ("bg", "Every track on Piece is royalty-free and licensed for streaming. Discover indie artists from Jamendo alongside our curated local library — all without copyright barriers.") , "Всяко парче в Piece е без авторски права и е лицензирано за стрийминг. Открий инди изпълнители от Jamendo както и от нашиата локална библиотека - напълно без копирайт бариери." },
			{ ("bg", "STATISTICS") , "СТАТИСТИКА" },
			{ ("bg", "Your listening,") , "Твоето слушане" },
			{ ("bg", "painted in color") , "оцветено" },
			{ ("bg", "Every genre has a color. Every day you listen builds a heatmap of your musical journey. See your most-played tracks, top genres, and listening patterns come to life visually.") , "Всеки жанр има цвят. Всеки ден, в който слушаш, сглобява шарена карта на твоето музикално пътуване. Виж своите най-слушани парчета и жанрове, както и тенденцията ти на слушане как оживяват." },
			{ ("bg", "Ready to experience music differently?") , "Готов ли си да преживееш музиката по различен начин?" },
			{ ("bg", "Get started here") , "Започни тук" },

			
		};
	}
}