# Раскладка by mysik

Напечатали «ghbdtn», а хотели «привет»? Нажмите одну клавишу, и программа сама сотрёт слово, напечатает его правильно и переключит раскладку.

**Страница программы и скачивание:** https://mysik-portfolio.vercel.app/soft/raskladka.html

## Что умеет

- **Pause** — исправить последнее слово или фразу. Shift+Pause — выделенный текст.
- **Для ноутбуков** — нет клавиши Pause — дважды Shift для слова, дважды Ctrl для выделенного.
- **Транслит** — ctrl+Alt+T: privet ⇄ привет.
- **Регистр** — ctrl+Alt+R: «пРИВЕТ» (забыли про Caps Lock) → «Привет».
- **Отмена** — нажали случайно — нажмите ещё раз, и слово вернётся.
- **Везде** — браузер, Telegram, Discord, Word, игры.

## Как начать

Нужна только Windows 10 или 11 — компилятор .NET Framework 4.x уже встроен в систему.

1. Скачайте репозиторий: зелёная кнопка **Code → Download ZIP**, и распакуйте.
2. Запустите `build.bat` — через пару секунд рядом появится `Raskladka.exe`.
3. Откройте `Raskladka.exe`. Права администратора не нужны.
4. **Включите «Запускать вместе с Windows».** Программа будет тихо работать в трее.
5. **Выберите вариант клавиш.** «Классика» с Pause или «Для ноутбуков».

## Полезно знать

- Не работает в окнах, запущенных от имени администратора, — так устроена защита Windows.

Файлы: `MainWindow.xaml`, `README.txt`, `Raskladka.cs`, `Shell.cs`, `Theme.xaml`, `app.ico`, `app.manifest`, `build.bat`.

## Другие программы by mysik

- [Zapret GUI](https://mysik-zapret.netlify.app) — Возвращает Discord, YouTube и Steam одной кнопкой. Без VPN: скорость и IP остаются вашими.
- [DNS Switch](https://mysik-portfolio.vercel.app/soft/dns-switch.html) — Меняет DNS в один клик и сам проверяет, какой быстрее именно у вас.
- [Пинг](https://mysik-portfolio.vercel.app/soft/ping.html) — Следит за интернетом каждую секунду и подсказывает, где проблема: Wi-Fi, роутер или провайдер.
- [Передача](https://mysik-portfolio.vercel.app/soft/peredacha.html) — Файлы, фото и тексты между компьютером и телефоном по QR-коду. Без проводов и облаков.
- [Video Loader](https://mysik-portfolio.vercel.app/soft/video-loader.html) — Скачивает видео и музыку с YouTube, VK Видео, RuTube и TikTok: до 4K, плейлисты, MP3 с обложкой.
- [Converter](https://mysik-portfolio.vercel.app/soft/converter.html) — Видео, музыка и картинки в нужный формат, включая HEIC с iPhone. Умеет сжимать перед отправкой.
- [Снимок](https://mysik-portfolio.vercel.app/soft/snimok.html) — Скриншот области по PrintScreen, стрелки, размытие и нумерация. Сразу в буфер.
- [Win Cleaner](https://mysik-portfolio.vercel.app/soft/win-cleaner.html) — Чистит мусор, убирает рекламу и Copilot, наводит порядок в автозагрузке. Всё можно вернуть.
- [Диск](https://mysik-portfolio.vercel.app/soft/disk.html) — Карта диска за минуту: видно, какие папки и файлы съели всё место.
- [Буфер](https://mysik-portfolio.vercel.app/soft/bufer.html) — Помнит всё, что вы копировали: текст, ссылки, картинки. Ctrl+Shift+V — и вставлено.
- [Визитка](https://mysik-portfolio.vercel.app/soft/vizitka.html) — Конструктор сайтов для мастеров: маникюр, барбер, массаж. Сайт под ключ за 15–30 минут.

Все программы — в портфолио: https://mysik-portfolio.vercel.app/#programs  
Сделано [mysik](https://mysik-portfolio.vercel.app).
