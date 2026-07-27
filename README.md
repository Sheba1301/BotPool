# 📁 Структура программы BotMinecraft

## Папка `Client`

| Файл / Папка | Назначение |
|--------------|------------|
| `Program.cs` | Главное меню, запуск ботов |
| `Accounts.cs` | Чтение и парсинг `config.ini` |
| `Directory.cs` | Определение путей к компилятору и конфигам |
| `config.ini` | Список аккаунтов (НИК ПАРОЛЬ ГРИФ) |
| `Client.csproj` | Проект C# |
| `bin/`, `obj/` | Временные/скомпилированные файлы |
| `publish_self_contained/` | Остаток от публикации (можно удалить) |

## Папка `Compiler`

| Файл / Папка | Назначение |
|--------------|------------|
| `BotPool.exe` | Исполняемый файл Minecraft Console Client |
| `MinecraftClient.ini` | Конфиг MCC |
| `lang/` | Языковые файлы MCC |
| `MinecraftClient.backup.ini` | Резервная копия конфига |

---

## ⚙️ Формат `config.ini`

НИК ПАРОЛЬ ГРИФ
Player1 123456 1
Player2 123456 2


---

## 🔗 Ссылки

- [Minecraft Console Client](https://github.com/MCCTeam/Minecraft-Console-Client)
- [Документация MCC](https://mccteam.github.io/)

---

## 📞 Контакты

[@Feykomet12](https://t.me/Feykomet12)
