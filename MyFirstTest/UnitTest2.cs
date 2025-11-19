using Dapper;
using MongoDB.Bson;
using MongoDB.Driver;
using Npgsql;
using RestSharp;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

//Как выглядит качественный автотест?
//1. Ключевые характеристики качественного автотеста

//· Детерминированность (Предсказуемость): Тест всегда проходит или всегда падает при одних и тех же условиях. Нет места случайностям (random data, неконтролируемые внешние вызовы).
//· Изолированность: Тест не зависит от других тестов и не оставляет после себя "мусора" (данных в БД, файлов и т.д.), который может повлиять на другие тесты.
//· Скорость: Тесты выполняются быстро. Это позволяет запускать их часто, в том числе в режиме разработки (pre-commit хуки).
//· Читаемость и простота: По коду теста должно быть сразу понятно, что он тестирует, какие данные использует и что ожидает в результате. Следует принципу Arrange-Act-Assert (или Given-When-Then).
//· Поддержание "чистого листа": Тест сам создает все необходимые для себя данные и сам их очищает после выполнения (через фикстуры или setUp/tearDown методы).
//· Проверка только одной вещи: Идеальный тест фокусируется на одном конкретном сценарии. Если нужно проверить несколько кейсов, пишется несколько тестов.
//· Надежность (Отсутствие "хлопающих" тестов): Тест не должен периодически падать без видимых причин. Это часто достигается через правильную изоляцию и мокирование.

//---

//2. Структура теста (Arrange-Act-Assert)

//Это классическая и самая понятная структура.

//1. Arrange (Подготовка): Подготовка всех необходимых данных, моков и состояния системы.
//   · Создание тестовых данных в БД.
//   · Настройка моков для внешних сервисов.
//   · Инициализация классов, входных параметров.
//2. Act (Действие): Вызов тестируемого метода или эндпоинта.
//   · userService.createUser(request)
//   · apiClient.post('/api/v1/orders', payload)
//3. Assert (Проверка): Проверка, что результат соответствует ожиданиям.
//   · Проверка возвращаемых данных.
//   · Проверка изменений в БД.
//   · Проверка вызова внешних сервисов с правильными параметрами (verify у моков).

namespace MyFirstTest
{
    public class UnitTest2
    {
        private const string _url = "http://45.130.147.122:5000"; //указываем адрес моего тестового стенда
        private const int _userId = 2;
        private RestClient _restClient;
        private string? sessionId; //так как сервис использует аутентификацию через хэдер SessionId, то нам обязательно нужно получать и передавать в запросы. Поэтому мы объявим переменную, а значение запишем потом
        private IMongoClient? _mongoClient; //подключаем библиотеку MongoDB через NuGet
        private IMongoDatabase? _database;
        private NpgsqlConnection? _connection; //подключаем библиотеку Npgsql и Dapper через NuGet
        //private LoggingRestClient _loggingClient; // Добавляем обертку для логирования
        private string connectionString = "mongodb://admin:admin@45.130.147.122:27017/?authSource=admin";
        private string _pgConnectionString = "Host=45.130.147.122;Port=5432;Database=testdb;Username=postgres;Password=postgres"; //пишем строку подключения в таком формате.

        // задача: нужно авторизоваться под тестовым пользователем и получить сессию, сохранить сессию в наше поле класса UnitTest2 и потом с помощью него создать пост и проверить, что в базе Монго появилась запись
        // обычно, такие штуки проверяют через БД, но пока усложнять не будем и подергаем ручки
        [SetUp]
        public async Task SetupAsync()
        {
            _mongoClient = new MongoClient(connectionString);
            _database = _mongoClient.GetDatabase("shootsy");
            _restClient = new RestClient();
            _connection = new NpgsqlConnection(_pgConnectionString); //подключаем библиотеку Npgsql и Dapper через NuGet
            await _connection.OpenAsync();

            var request = new RestRequest($"{_url}/Users/sign-in", Method.Post);
            var json = @"{
                ""login"": ""123"",
                ""password"": ""123123123""
            }";
            request.AddBody(json); // добавляем тело в формате текста в виде джейсон. Джейсоны это всегда строки
            var response = await _restClient.ExecuteAsync(request);
            sessionId = response.Content;
        }

        [TearDown]
        public void Teardown()
        {
            _restClient?.Dispose();
            _mongoClient?.Dispose();
            _connection?.Dispose();
        }

        [Test]
        public async Task CreatePostRequest()
        {
            // важное правило написания кода - это если ты где то что то используешь больше одного раза, то нужно создавать переменную
            // в нашем случае мы используем данные при создании И далее используем их при сравнении. Так удобнее и в случае чего правится в одном месте
            var text = "это мой текст";
            var collection = _database?.GetCollection<BsonDocument>("Post"); // тут мы говорим, что из нашей базы будем работать с коллекцией Post и результат получать в формате BsonDocument
                                                                             //это особый формат в mongodb, который типа как json, но структура данных там иная

            //            {
            //                "id": "507f1f77bcf86cd799439011",
            //  "name": "John Doe",
            //  "age": 30,
            //  "isActive": true,
            //  "hobbies": ["reading", "gaming"],
            //  "address": {
            //                    "city": "Moscow",
            //    "zip": "123456"
            //  },
            //  "birthDate": "1990-01-01"
            //}

            //            var document = new BsonDocument
            //{
            //    { "_id", ObjectId.Parse("507f1f77bcf86cd799439011") },
            //    { "name", "John Doe" },
            //    { "age", 30 },
            //    { "isActive", true },
            //    { "hobbies", new BsonArray { "reading", "gaming" } },
            //    { "address", new BsonDocument
            //        {
            //            { "city", "Moscow" },
            //            { "zip", "123456" }
            //        }
            //    },
            //    { "birthDate", new DateTime(1990, 1, 1) },
            //    { "salary", 1000.50m },
            //    { "data", new byte[] { 1, 2, 3, 4, 5 } } // Бинарные данные
            //}
            var request = new RestRequest($"{_url}/Posts", Method.Post);
            request.AddHeader("Session", sessionId); // с помощью данного метода AddHeader мы добавляем нашему существующему запросу хэдер вида ключ - значение
                                                     //ключ это имя хэдера а значение....это его значение
            var json = @$"{{
                ""idUser"": {_userId}, 
                ""text"": ""{text}""
            }}";
            //Показать как можно это сериализовать

            var reqBodyObj = new CreatePostModel { idUser = _userId, text = text };
            var jsonBody = JsonSerializer.Serialize(reqBodyObj);
            //и затем так же можем добавить полученный json в запрос

            request.AddBody(json);

            var response = await _restClient.ExecuteAsync(request);  // проверим, что статус код ожидаем

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"При создании запроса возникла ошибка:{Environment.NewLine}{response.ErrorMessage}");

            var idPost = response.Content?.Trim('"'); //на данный момент сессия у меня возвращается с ковычками, поэтому чтобы правильно ее записать я вызову метод у строк Trim и обрежу кавычки

            var filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(idPost)); //здесь мы создаем фильтр, по которому в Mongo будет искаться запись.
            var mongoRecord = await collection.Find(filter).FirstOrDefaultAsync(); //вызываем команду поиска и получаем первое попавшееся значение и если его нет - то  null

            Assert.That(mongoRecord, Is.Not.Null, "Запись не найдена в MongoDB"); //проверяем, что у нас вернулся не нулл, иначе ошибка
            Assert.That(mongoRecord["IdUser"].AsInt32, Is.EqualTo(_userId), "idUser не совпадает"); //смотрим на значение поля IdUser и делаем его тип AsInt32 целочисленным и сравниваем с ожидаемым
            Assert.That(mongoRecord["Text"].AsString, Is.EqualTo(text), "Текст поста не совпадает"); //у bson значения хранятся как пара ключ значение, поэтому можно писать ключ и вместо него будет подставляться значение
        }

        [Test] //Сейчас попробуем отредактировать пользователя через PATCH и затем получить в постгре запись для сверки
        public async Task UpdateUserTest()
        {
            var lastname = "Бородач";
            var firstname = "Александр";

            var request = new RestRequest($"{_url}/users/{_userId}", Method.Patch);
            var json = @$"[
                  {{
                    ""op"": ""replace"",
                    ""path"": ""/lastname"",
                    ""value"": ""{lastname}""
                  }},
                  {{
                    ""op"": ""replace"",
                    ""path"": ""/firstname"",
                    ""value"": ""{firstname}""
                  }}
                ]";
            Console.WriteLine(json);
            request.AddBody(json);
            request.AddHeader("Session", sessionId);
            request.AddHeader("Content-Type", "application/json-patch+json"); //тут нужно явно передать Content-Type в заголовке

            var response = await _restClient.ExecuteAsync(request);


            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                $"PATCH запрос вернул ошибку: {response}");

            var userQuery = $"SELECT firstname, lastname FROM security.users WHERE id = {_userId}"; //здесь мы с вами пишем sql запрос в виде строки и подставляем юзер ид
            var updatedUser = await _connection.QueryFirstOrDefaultAsync<(string FirstName, string LastName)>(userQuery); //здесь мы получаем ответ от базы и говорим, что мы получим два значения с именами 
            // FirstName и LastName от запроса userQuery. Потом мы можем использовать из объекта ответа updatedUser через точку получать свойства FirstName и LastName.

            Assert.That(updatedUser.FirstName, Is.EqualTo(firstname), "Имя не обновилось в PostgreSQL");
            Assert.That(updatedUser.LastName, Is.EqualTo(lastname), "Фамилия не обновилась в PostgreSQL");
        }


        // Сейчас с вами сделаем более лаконичный вариант работы с сущностями через модели. Создадим модель PostModel, которую можно либо самим собрать, либо идти в код разрабов и искать модель там 
        [Test]
        public async Task DeletePost()
        {
            var text = "это мой текст";

            var collection = _database?.GetCollection<PostModel>("Post"); //так же получаем колекцию Post, но уже работаем с ней не как с типом Bson, а как модель PostModel, которую мы создали

            var testPost = new PostModel // создаем новый объект PostModel и даем на него ссылку в переменную testPost
            {
                CreateDate = DateTime.Now, //через DateTime.Now можно получить точную дату в момент выполнения кода
                EditDate = DateTime.Now,
                IdUser = _userId, //тут передаем значения с типами данных, которые соответствуют нашей модели
                Text = text
            };

            await collection.InsertOneAsync(testPost); //здесь мы с вами создаем запись в базе и передаем созданный объект полностью. Заметьте, что мы при создании модели не давали значения полю ID
                                                       //а прикол в том, что после добавления записи в монго, драйвер сам вставит в вашу модель идентификатор созданной записи. Поэтому после инсерта, мы можем обратиться к той же модели и забрать идентификатор

            var request = new RestRequest($"{_url}/Posts/{testPost.Id}", Method.Delete); //выбираем метод delete
            request.AddHeader("Session", sessionId); //не забываем про хэдер

            var response = await _restClient.ExecuteAsync(request);  //выполняем запрос

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent), $"При создании запроса возникла ошибка:{Environment.NewLine}{response.ErrorMessage}");
            //проверяем статус код обязательно

            var mongoRecord = await collection.Find(x => x.Id == testPost.Id).FirstOrDefaultAsync(); //пытаемся по айдишнику получить запись. Но, так как мы теперь не с абстрактным BSON работаем
            //а с моделью, поэтому можем сразу обратиться как через linq к нужному полю объекта и сравнить. И вернется либо первая запись по совпадению, либо null

            Assert.That(mongoRecord, Is.Null, "Запись не удалена в MongoDB"); //мы ожидаем именно null, так как проверяем удаление записи и из за чего именно нулл является валидным
        }

        //Как можно еще упростить работу с json? Чтобы можно было создавать объект, "паковать его в json" и отправлять сервисам. И наоборот при получении json преобразовывать его в объект?
        // для этого есть такие процессы как
        //Сериализация - преобразование объекта в формат для хранения/передачи(JSON, BSON, XML)
        //Десериализация - восстановление объекта из формата

        //Пример:
        //using System.Text.Json;

        //public class User
        //    {
        //        public int Id { get; set; }
        //        public string Name { get; set; }
        //        public string Email { get; set; }
        //        public DateTime CreatedAt { get; set; }
        //    }

        //    // Сериализация
        //    var user = new User { Id = 1, Name = "John", Email = "john@example.com", CreatedAt = DateTime.UtcNow };
        //    string json = JsonSerializer.Serialize(user);
        //    // Результат: {"Id":1,"Name":"John","Email":"john@example.com","CreatedAt":"2024-01-15T10:30:00Z"}

        //    // Десериализация
        //    string jsonString = "{\"Id\":1,\"Name\":\"John\",\"Email\":\"john@example.com\"}";
        //    User deserializedUser = JsonSerializer.Deserialize<User>(jsonString);

        // Сейчас с вами сделаем более лаконичный вариант работы с сущностями через модели. Создадим модель PostModel, которую можно либо самим собрать, либо идти в код разрабов и искать модель там 
        [Test]
        public async Task GetUserWithModel()
        {
            //const string sql = @$"INSERT INTO ""security"".users
            //(create_date, edit_date, ""Login"", password,  gender_id, city_id, firstname, lastname, discription, it_profession_id, is_delete)
            //VALUES(CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'login', 123 , 1, 1, 'firstname', 'lastname', 'description', 1, false)";

            //await using var command = new NpgsqlCommand(sql, _connection);

            //// Выполняем и возвращаем ID новой записи
            //var newId = await _connection.ExecuteAsync(sql);

            //Console.WriteLine($"Created user with ID: {newId}");


            var request = new RestRequest($"{_url}/Users/{_userId}", Method.Get); //выбираем метод delete
            request.AddHeader("Session", sessionId); //не забываем про хэдер
            var response = await _restClient.ExecuteAsync(request);  //выполняем запрос
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"При выполнении запроса возникла ошибка:{Environment.NewLine}{response.ErrorMessage}");

            var obj = JsonSerializer.Deserialize<UserModel>(response.Content);
        }
    }

    class CreatePostModel
    {
        public int idUser;
        public string text;
    }

    class UserModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("createDate")]
        public DateTime? CreateDate { get; set; }
        [JsonPropertyName("editDate")]
        public DateTime? EditDate { get; set; }
        [JsonPropertyName("login")]
        public string? Login { get; set; }
        [JsonPropertyName("firstname")]
        public string? Firstname { get; set; }
        [JsonPropertyName("lastname")]
        public string? Lastname { get; set; }
        [JsonPropertyName("gender")]
        public string? Gender { get; set; }
        [JsonPropertyName("city")]
        public string? City { get; set; }
    }
}