-- Initial Companies

CREATE TABLE companies (
  company_id INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
  company_name VARCHAR(32) NOT NULL,
  bank_account_number VARCHAR(12)
);

INSERT INTO companies (company_name)
VALUES
  ('electronics-supplier'),
  ('screen-supplier'),
  ('case-supplier'),
  ('bulk-logistics'),
  ('consumer-logistics'),
  ('pear-company'),
  ('sumsang-company'),
  ('commercial-bank'),
  ('retail-bank'),
  ('thoh'),
  ('recycler');

-- Table: order_statuses
-- Order statuses lookup table
-- Enum datatype didn't work with Entity Framework
CREATE TABLE order_statuses (
  status_id INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY NOT NULL,
  status VARCHAR(16) UNIQUE NOT NULL
);

INSERT INTO order_statuses (status)
VALUES
  ('PENDING'),
  ('ACCEPTED'),
  ('REJECTED'),
  ('IN_PROGRESS'),
  ('COMPLETED'),
  ('IN_TRANSIT'),
  ('DISASTER'),
  ('EXPIRED');

-- Table: materials
-- The raw materials we order to convert into electronics
-- Lookup table
CREATE TABLE materials (
  material_id INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY NOT NULL,
  material_name VARCHAR(16) UNIQUE NOT NULL,
  price_per_kg NUMERIC(10,3)
);

CREATE TABLE material_orders (
  order_id INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY NOT NULL,
  supplier_id INT NOT NULL,
  external_order_id INT,
  pickup_request_id INT,
  material_id INT NOT NULL,
  remaining_amount INT NOT NULL,
  order_status INT NOT NULL DEFAULT 1,
  ordered_at NUMERIC(1000,3) NOT NULL,
  received_at NUMERIC(1000,3)
);

ALTER TABLE material_orders ADD FOREIGN KEY (supplier_id) REFERENCES companies (company_id);
ALTER TABLE material_orders ADD FOREIGN KEY (order_status) REFERENCES order_statuses (status_id);
ALTER TABLE material_orders ADD FOREIGN KEY (material_id) REFERENCES materials (material_id);

-- Table: material_supplies
-- How much of the materials we have in our stock
CREATE TABLE material_supplies (
  supply_id INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY NOT NULL,
  material_id INT NOT NULL,
  received_at NUMERIC(1000,3) NOT NULL,
  processed_at NUMERIC(1000,3)
);

ALTER TABLE material_supplies ADD FOREIGN KEY (material_id) REFERENCES materials (material_id);

-- Machines

-- Table: machine_statuses
-- Lookup table for the status of a machine
-- Because Entity Framework can't handle enums
CREATE TABLE machine_statuses (
  status_id INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
  status VARCHAR(8) UNIQUE NOT NULL
);

-- Initial values for machine statuses
INSERT INTO machine_statuses (status)
VALUES
  ('STANDBY'),
  ('IN_USE'),
  ('BROKEN');

-- Table: machine_orders
-- For us to store the machine orders we place
CREATE TABLE machine_orders (
  order_id INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY NOT NULL,
  supplier_id INT NOT NULL,
  external_order_id INT,
  pickup_request_id INT,
  remaining_amount INT NOT NULL,
  order_status INT NOT NULL DEFAULT 1,
  placed_at NUMERIC(1000,3) NOT NULL,
  received_at NUMERIC(1000,3)
);

ALTER TABLE machine_orders ADD FOREIGN KEY (order_status) REFERENCES order_statuses (status_id);

-- Table: machines
-- For storing the details of the machines we currently have in our possession
CREATE TABLE machines (
  machine_id INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY NOT NULL,
  -- output_amount INT NOT NULL,
  machine_status INT NOT NULL DEFAULT 1,
  purchase_price FLOAT NOT NULL,
  purchased_at NUMERIC(1000,3) NOT NULL,
  received_at NUMERIC(1000,3),
  removed_at NUMERIC(1000,3),
  order_id INT NOT NULL
);

ALTER TABLE machines ADD FOREIGN KEY (machine_status) REFERENCES machine_statuses (status_id);
ALTER TABLE machines ADD FOREIGN KEY (order_id) REFERENCES machine_orders (order_id);

-- Table: machine_ratios
-- The ratio of a specific raw material for a machine
CREATE TABLE machine_ratios (
  ratio_id INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY NOT NULL,
  material_id INT NOT NULL,
  ratio INT NOT NULL
);

ALTER TABLE machine_ratios ADD FOREIGN KEY (material_id) REFERENCES materials (material_id);

CREATE TABLE machine_details (
  detail_id INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY NOT NULL,
  maximum_output INT NOT NULL,  -- the maximum amount of electronics that can be produced by a machine in a single day
  ratio_id INT NOT NULL
);

ALTER TABLE machine_details ADD FOREIGN KEY (ratio_id) REFERENCES machine_ratios (ratio_id);

-- Electronics

CREATE TABLE electronics_statuses (
  status_id INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY NOT NULL,
  status VARCHAR(12) NOT NULL
);

INSERT INTO electronics_statuses (status)
VALUES
  ('AVAILABLE'),
  ('RESERVED');

-- Table: electronics
-- Our current stock of electronics and the details of each unit
CREATE TABLE electronics (
  electronic_id INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY NOT NULL,
  produced_at NUMERIC(1000,3) NOT NULL,
  electronics_status INT NOT NULL DEFAULT 1,
  sold_at NUMERIC(1000,3)
);

ALTER TABLE electronics ADD FOREIGN KEY (electronics_status) REFERENCES electronics_statuses (status_id);

-- Table: electronics_orders
-- Orders that companies place with us to order electronics
CREATE TABLE electronics_orders (
  order_id INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY NOT NULL,
  order_status INT NOT NULL DEFAULT 1,
  manufacturer_id INT NOT NULL,
  total_amount INT NOT NULL,
  remaining_amount INT NOT NULL,
  ordered_at NUMERIC(1000,3) NOT NULL,
  processed_at NUMERIC(1000,3)
);

ALTER TABLE electronics_orders ADD FOREIGN KEY (manufacturer_id) REFERENCES companies (company_id);
ALTER TABLE electronics_orders ADD FOREIGN KEY (order_status) REFERENCES order_statuses (status_id);

-- Miscellaneous Lookups
 
CREATE TABLE lookup_values (
  value_id INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY NOT NULL,
  electronics_price_per_unit NUMERIC(10,3) NOT NULL DEFAULT 1000,
  changed_at NUMERIC(1000,3) NOT NULL
);

CREATE TABLE simulation (
  simulation_id INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY NOT NULL,
  day_number INT NOT NULL DEFAULT 0,
  started_at TIMESTAMPTZ DEFAULT NOW(),
  is_running BOOLEAN NOT NULL DEFAULT TRUE
);

-- CREATE TABLE logistics (
--   logistic_id INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY NOT NULL,
--   logistics_reference_number INT NOT NULL,

-- )

CREATE TABLE disasters (
  disaster_id INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY NOT NULL,
  broken_at NUMERIC(1000,3) NOT NULL,
  machines_affected INT NOT NULL
);

CREATE TABLE payments (
  payment_id INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY NOT NULL,
  transaction_number VARCHAR(64) NOT NULL,
  status VARCHAR(16) NOT NULL,
  amount NUMERIC(10,3) NOT NULL,
  timestamp NUMERIC(1000,3) NOT NULL,
  description VARCHAR(256),
  from_account VARCHAR(32) NOT NULL,
  to_account VARCHAR(32) NOT NULL,
  order_id INT,
  CONSTRAINT fk_order FOREIGN KEY(order_id) REFERENCES electronics_orders(order_id)
);
