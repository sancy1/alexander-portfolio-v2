// // internal/config/config.go
package config

import (
	"fmt"
	"strconv"
	"strings"
	"time"

	"github.com/joho/godotenv"
	"github.com/spf13/viper"
)

type Config struct {
	Server   ServerConfig   `mapstructure:"server"`
	Database DatabaseConfig `mapstructure:"database"`
	Kafka    KafkaConfig    `mapstructure:"kafka"`
	Redis    RedisConfig    `mapstructure:"redis"`
	JWT      JWTConfig      `mapstructure:"jwt"`
	App      AppConfig      `mapstructure:"app"`
	Swagger  SwaggerConfig  `mapstructure:"swagger"`
	RabbitMQ RabbitMQConfig `mapstructure:"rabbitmq"`
}

type ServerConfig struct {
	Port         string        `mapstructure:"port"`
	Mode         string        `mapstructure:"mode"`
	ReadTimeout  time.Duration `mapstructure:"read_timeout"`
	WriteTimeout time.Duration `mapstructure:"write_timeout"`
}

type DatabaseConfig struct {
	Host              string        `mapstructure:"host"`
	Port              string        `mapstructure:"port"`
	User              string        `mapstructure:"user"`
	Password          string        `mapstructure:"password"`
	DBName            string        `mapstructure:"dbname"`
	SSLMode           string        `mapstructure:"sslmode"`
	MaxOpenConns      int           `mapstructure:"max_open_conns"`
	MaxIdleConns      int           `mapstructure:"max_idle_conns"`
	ConnMaxLifetime   time.Duration `mapstructure:"conn_max_lifetime"`
	ConnMaxIdleTime   time.Duration `mapstructure:"conn_max_idle_time"`
	MaxRetries        int           `mapstructure:"max_retries"`
	RetryDelay        time.Duration `mapstructure:"retry_delay"`
	BackoffMultiplier float64       `mapstructure:"backoff_multiplier"`
	Provider          string        `mapstructure:"provider"`
	DatabaseURL       string        `mapstructure:"database_url"`
}

func (c *DatabaseConfig) DSN() string {
	if c.DatabaseURL != "" {
		return c.DatabaseURL
	}
	return fmt.Sprintf("host=%s port=%s user=%s password=%s dbname=%s sslmode=%s",
		c.Host, c.Port, c.User, c.Password, c.DBName, c.SSLMode)
}

type KafkaConfig struct {
	Brokers       []string `mapstructure:"brokers"`
	Topic         string   `mapstructure:"topic"`
	ConsumerGroup string   `mapstructure:"consumer_group"`
	Username      string   `mapstructure:"username"`
	Password      string   `mapstructure:"password"`
	SASLMechanism string   `mapstructure:"sasl_mechanism"`
}

type RedisConfig struct {
	Host     string `mapstructure:"host"`
	Port     string `mapstructure:"port"`
	Password string `mapstructure:"password"`
	DB       int    `mapstructure:"db"`
}

func (c *RedisConfig) Addr() string {
	return fmt.Sprintf("%s:%s", c.Host, c.Port)
}

type JWTConfig struct {
	Secret string `mapstructure:"secret"`
	Issuer string `mapstructure:"issuer"`
}

type AppConfig struct {
	Env     string `mapstructure:"env"`
	Name    string `mapstructure:"name"`
	Version string `mapstructure:"version"`
}

type SwaggerConfig struct {
	AdminUser     string `mapstructure:"admin_user"`
	AdminPassword string `mapstructure:"admin_password"`
}

type RabbitMQConfig struct {
	Host     string `mapstructure:"host"`
	Port     string `mapstructure:"port"`
	Username string `mapstructure:"username"`
	Password string `mapstructure:"password"`
	VHost    string `mapstructure:"virtual_host"`
}

func Load(configPath string) (*Config, error) {
	_ = godotenv.Load()
	_ = godotenv.Load("../.env")
	_ = godotenv.Load("../../.env")

	viper.SetConfigName("config")
	viper.SetConfigType("yaml")
	if configPath != "" {
		viper.AddConfigPath(configPath)
	}
	viper.AddConfigPath(".")
	viper.AddConfigPath("./configs")

	viper.AutomaticEnv()
	viper.SetEnvKeyReplacer(strings.NewReplacer(".", "_"))

	setDefaults()
	bindViperExplicitKeys()

	if err := viper.ReadInConfig(); err != nil {
		if _, ok := err.(viper.ConfigFileNotFoundError); !ok {
			return nil, fmt.Errorf("failed to read operational yaml profile: %w", err)
		}
	}

	var config Config
	if err := viper.Unmarshal(&config); err != nil {
		return nil, fmt.Errorf("unmarshal error mapping configurations struct: %w", err)
	}

	bindEnvOverrides(&config)
	return &config, nil
}

func setDefaults() {
	viper.SetDefault("server.port", "8081")
	viper.SetDefault("server.mode", "release")
	viper.SetDefault("server.read_timeout", 30*time.Second)
	viper.SetDefault("server.write_timeout", 30*time.Second)
	viper.SetDefault("database.max_open_conns", 25)
	viper.SetDefault("database.max_idle_conns", 10)
	viper.SetDefault("database.sslmode", "require")
	viper.SetDefault("database.max_retries", 6)
	viper.SetDefault("database.retry_delay", 2*time.Second)
	viper.SetDefault("database.backoff_multiplier", 1.5)
	viper.SetDefault("database.conn_max_lifetime", 30*time.Minute)
	viper.SetDefault("database.conn_max_idle_time", 5*time.Minute)
	// Redis defaults - ADDED
	viper.SetDefault("redis.host", "localhost")
	viper.SetDefault("redis.port", "6379")
	viper.SetDefault("redis.db", 0)
	viper.SetDefault("app.env", "development")
	viper.SetDefault("swagger.admin_user", "admin")
	viper.SetDefault("swagger.admin_password", "swagger-secure-password-2026")
	viper.SetDefault("kafka.brokers", []string{"localhost:9092"})
	viper.SetDefault("kafka.topic", "notifications")
	viper.SetDefault("kafka.consumer_group", "notification-service")
	viper.SetDefault("rabbitmq.host", "localhost")
	viper.SetDefault("rabbitmq.port", "5672")
	viper.SetDefault("rabbitmq.username", "guest")
	viper.SetDefault("rabbitmq.password", "guest")
	viper.SetDefault("rabbitmq.virtual_host", "/")
}

func bindViperExplicitKeys() {
	_ = viper.BindEnv("server.port", "SERVER_PORT")
	_ = viper.BindEnv("server.mode", "SERVER_MODE")
	_ = viper.BindEnv("app.env", "APP_ENV")
	_ = viper.BindEnv("app.name", "APP_NAME")
	_ = viper.BindEnv("app.version", "APP_VERSION")
	_ = viper.BindEnv("DATABASE_URL")
	_ = viper.BindEnv("DB_HOST")
	_ = viper.BindEnv("DB_PORT")
	_ = viper.BindEnv("DB_USER")
	_ = viper.BindEnv("DB_PASSWORD")
	_ = viper.BindEnv("DB_NAME")
	_ = viper.BindEnv("DB_SSLMODE")
	_ = viper.BindEnv("swagger.admin_user", "SWAGGER_ADMIN_USER")
	_ = viper.BindEnv("swagger.admin_password", "SWAGGER_ADMIN_PASSWORD")
	_ = viper.BindEnv("kafka.brokers", "KAFKA_BROKERS")
	_ = viper.BindEnv("kafka.topic", "KAFKA_TOPIC")
	_ = viper.BindEnv("kafka.consumer_group", "KAFKA_CONSUMER_GROUP")
	_ = viper.BindEnv("kafka.username", "KAFKA_USERNAME")
	_ = viper.BindEnv("kafka.password", "KAFKA_PASSWORD")
	_ = viper.BindEnv("kafka.sasl_mechanism", "KAFKA_SASL_MECHANISM")
	// Redis environment bindings - ADDED
	_ = viper.BindEnv("redis.host", "REDIS_HOST")
	_ = viper.BindEnv("redis.port", "REDIS_PORT")
	_ = viper.BindEnv("redis.password", "REDIS_PASSWORD")
	_ = viper.BindEnv("redis.db", "REDIS_DB")
	// RabbitMQ environment bindings
	_ = viper.BindEnv("rabbitmq.host", "RABBITMQ_HOST")
	_ = viper.BindEnv("rabbitmq.port", "RABBITMQ_PORT")
	_ = viper.BindEnv("rabbitmq.username", "RABBITMQ_USERNAME")
	_ = viper.BindEnv("rabbitmq.password", "RABBITMQ_PASSWORD")
	_ = viper.BindEnv("rabbitmq.virtual_host", "RABBITMQ_VIRTUAL_HOST")
}

func bindEnvOverrides(cfg *Config) {
	if port := viper.GetString("SERVER_PORT"); port != "" {
		cfg.Server.Port = port
	}
	if mode := viper.GetString("SERVER_MODE"); mode != "" {
		cfg.Server.Mode = mode
	}
	if env := viper.GetString("APP_ENV"); env != "" {
		cfg.App.Env = env
	}
	if name := viper.GetString("APP_NAME"); name != "" {
		cfg.App.Name = name
	}
	if version := viper.GetString("APP_VERSION"); version != "" {
		cfg.App.Version = version
	}
	if dbURL := viper.GetString("DATABASE_URL"); dbURL != "" {
		cfg.Database.DatabaseURL = dbURL
	}
	if host := viper.GetString("DB_HOST"); host != "" {
		cfg.Database.Host = host
	}
	if port := viper.GetString("DB_PORT"); port != "" {
		cfg.Database.Port = port
	}
	if user := viper.GetString("DB_USER"); user != "" {
		cfg.Database.User = user
	}
	if pass := viper.GetString("DB_PASSWORD"); pass != "" {
		cfg.Database.Password = pass
	}
	if name := viper.GetString("DB_NAME"); name != "" {
		cfg.Database.DBName = name
	}
	if ssl := viper.GetString("DB_SSLMODE"); ssl != "" {
		cfg.Database.SSLMode = ssl
	}

	if maxOpenStr := viper.GetString("DB_MAX_OPEN_CONNS"); maxOpenStr != "" {
		if val, err := strconv.Atoi(maxOpenStr); err == nil {
			cfg.Database.MaxOpenConns = val
		}
	}
	if maxIdleStr := viper.GetString("DB_MAX_IDLE_CONNS"); maxIdleStr != "" {
		if val, err := strconv.Atoi(maxIdleStr); err == nil {
			cfg.Database.MaxIdleConns = val
		}
	}

	if swaggerUser := viper.GetString("SWAGGER_ADMIN_USER"); swaggerUser != "" {
		cfg.Swagger.AdminUser = swaggerUser
	}
	if swaggerPass := viper.GetString("SWAGGER_ADMIN_PASSWORD"); swaggerPass != "" {
		cfg.Swagger.AdminPassword = swaggerPass
	}

	if brokers := viper.GetString("KAFKA_BROKERS"); brokers != "" {
		cfg.Kafka.Brokers = strings.Split(brokers, ",")
	}
	if topic := viper.GetString("KAFKA_TOPIC"); topic != "" {
		cfg.Kafka.Topic = topic
	}
	if group := viper.GetString("KAFKA_CONSUMER_GROUP"); group != "" {
		cfg.Kafka.ConsumerGroup = group
	}
	if user := viper.GetString("KAFKA_USERNAME"); user != "" {
		cfg.Kafka.Username = user
	}
	if pass := viper.GetString("KAFKA_PASSWORD"); pass != "" {
		cfg.Kafka.Password = pass
	}
	if saslMech := viper.GetString("KAFKA_SASL_MECHANISM"); saslMech != "" {
		cfg.Kafka.SASLMechanism = saslMech
	}

	// Redis environment overrides - ADDED
	if host := viper.GetString("REDIS_HOST"); host != "" {
		cfg.Redis.Host = host
	}
	if port := viper.GetString("REDIS_PORT"); port != "" {
		cfg.Redis.Port = port
	}
	if password := viper.GetString("REDIS_PASSWORD"); password != "" {
		cfg.Redis.Password = password
	}
	if dbStr := viper.GetString("REDIS_DB"); dbStr != "" {
		if db, err := strconv.Atoi(dbStr); err == nil {
			cfg.Redis.DB = db
		}
	}

	// RabbitMQ environment overrides
	if host := viper.GetString("RABBITMQ_HOST"); host != "" {
		cfg.RabbitMQ.Host = host
	}
	if port := viper.GetString("RABBITMQ_PORT"); port != "" {
		cfg.RabbitMQ.Port = port
	}
	if username := viper.GetString("RABBITMQ_USERNAME"); username != "" {
		cfg.RabbitMQ.Username = username
	}
	if password := viper.GetString("RABBITMQ_PASSWORD"); password != "" {
		cfg.RabbitMQ.Password = password
	}
	if vhost := viper.GetString("RABBITMQ_VIRTUAL_HOST"); vhost != "" {
		cfg.RabbitMQ.VHost = vhost
	}

	targetHost := cfg.Database.Host
	if cfg.Database.DatabaseURL != "" {
		targetHost = cfg.Database.DatabaseURL
	}

	if strings.Contains(targetHost, "neon.tech") {
		cfg.Database.Provider = "neon"
		if cfg.Database.MaxOpenConns > 5 {
			cfg.Database.MaxOpenConns = 5
		}
	} else if strings.Contains(targetHost, "localhost") || strings.Contains(targetHost, "127.0.0.1") {
		cfg.Database.Provider = "local"
	} else {
		cfg.Database.Provider = "standard"
	}
}
