CREATE TABLE roles (
    role_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE users (
    user_id SERIAL PRIMARY KEY,
    login VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    nickname VARCHAR(100) NOT NULL,
    balance NUMERIC(10,2) DEFAULT 0,
    role_id INT NOT NULL REFERENCES roles(role_id) ON DELETE RESTRICT,
    avatar_url TEXT
);

CREATE TABLE developers (
    developer_id SERIAL PRIMARY KEY,
    name VARCHAR(150) NOT NULL UNIQUE
);

CREATE TABLE genres (
    genre_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE
);


CREATE TABLE games (
    game_id SERIAL PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    description TEXT,
    developer_id INT NOT NULL REFERENCES developers(developer_id) ON DELETE CASCADE,
    cover_url TEXT,
    release_date DATE NOT NULL,
    download_url TEXT NOT NULL,
    price NUMERIC(10,2) NOT NULL CHECK (price >= 0),
    genre_id INT NOT NULL REFERENCES genres(genre_id) ON DELETE RESTRICT
);

CREATE TABLE purchases (
    purchase_id SERIAL PRIMARY KEY,
    user_id INT NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    game_id INT NOT NULL REFERENCES games(game_id) ON DELETE CASCADE,
    purchase_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);


INSERT INTO roles(name) VALUES
  ('Admin'),
  ('Developer'),
  ('User')

  ---
  

insert into genres (name) values
('Файтинг')

insert into developers (name) values
('NEOWIZ')
  
insert into games (title,description,developer_id,cover_url,release_date,download_url,price,genre_id) values
('Lies of P','Lies of P — захватывающая игра в жанре soulslike, в которой знакомый сюжет сказки «Пиноккио» развернется в мрачных, но элегантных декорациях Прекрасной эпохи.',1,'https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/1627720/header.jpg?t=1754552654','2023-09-18','https://stars-clicker.ru/pon.rar',2000,1)
