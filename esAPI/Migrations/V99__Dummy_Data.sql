-- Comprehensive dummy data for testing the Electronics Supplier API

-- ============================================================================
-- MATERIALS - Raw materials with pricing
-- ============================================================================

INSERT INTO materials (material_name, price_per_kg) VALUES
('copper', 8.50),
('silicone', 12.75),
('aluminum', 1.85),
('plastic', 2.30),
('sand', 0.15);

-- ============================================================================
-- MACHINE RATIOS - Production requirements (copper:silicone = 3:2 ratio)
-- ============================================================================

-- Copper ratio for electronics production
INSERT INTO machine_ratios (material_id, ratio) VALUES
((SELECT material_id FROM materials WHERE material_name = 'copper'), 3),
((SELECT material_id FROM materials WHERE material_name = 'silicone'), 2);

-- ============================================================================
-- MACHINE DETAILS - Machine specifications
-- ============================================================================

-- High-capacity machine (100 units/day capacity)
INSERT INTO machine_details (maximum_output, ratio_id) VALUES
(100, 1),  -- Uses copper ratio
(100, 2);  -- Uses silicone ratio

-- ============================================================================
-- SIMULATION - Initialise simulation state
-- ============================================================================

INSERT INTO simulation (day_number, started_at, is_running) VALUES
(15, NOW(), true);

-- ============================================================================
-- LOOKUP VALUES - Pricing configuration
-- ============================================================================

INSERT INTO lookup_values (electronics_price_per_unit, changed_at) VALUES
(25.50, 1.0),   -- Initial pricing
(26.00, 8.0),   -- Price increase on day 8
(25.75, 12.0);  -- Price adjustment on day 12

-- ============================================================================
-- MACHINE ORDERS - Historical machine purchases
-- ============================================================================

INSERT INTO machine_orders (supplier_id, external_order_id, remaining_amount, order_status, placed_at, received_at) VALUES
-- Orders from THoH (supplier_id = 10)
(10, 1001, 2, 5, 1.0, 3.0),   -- COMPLETED order
(10, 1002, 1, 5, 2.0, 4.0),   -- COMPLETED order
(10, 1003, 3, 5, 5.0, 7.0),   -- COMPLETED order
(10, 1004, 2, 4, 10.0, NULL), -- IN_PROGRESS order
(10, 1005, 1, 1, 12.0, NULL); -- PENDING order

-- ============================================================================
-- MACHINES - Manufacturing equipment inventory
-- ============================================================================

INSERT INTO machines (machine_status, purchase_price, purchased_at, received_at, removed_at, order_id) VALUES
-- Operational machines
(1, 15000.00, 1.0, 3.0, NULL, 1),  -- STANDBY
(1, 15000.00, 2.0, 4.0, NULL, 2),  -- STANDBY
(3, 15000.00, 1.0, 3.0, NULL, 1);  -- BROKEN

-- ============================================================================
-- MATERIAL ORDERS - Raw material purchase history
-- ============================================================================

INSERT INTO material_orders (supplier_id, external_order_id, material_id, remaining_amount, order_status, ordered_at, received_at) VALUES
-- Copper orders from THoH
(10, 2001, 1, 0, 5, 1.0, 2.0),     -- COMPLETED - 500 units delivered
(10, 2002, 1, 0, 5, 3.0, 4.0),     -- COMPLETED - 300 units delivered
(10, 2003, 1, 150, 6, 8.0, 9.0),   -- IN_TRANSIT - 150 remaining
(10, 2004, 1, 400, 1, 12.0, NULL), -- PENDING - 400 remaining
(10, 2005, 1, 250, 1, 14.0, NULL), -- PENDING - 250 remaining

-- Silicone orders from THoH
(10, 2006, 2, 0, 5, 1.0, 2.0),     -- COMPLETED - 400 units delivered
(10, 2007, 2, 0, 5, 4.0, 5.0),     -- COMPLETED - 200 units delivered
(10, 2008, 2, 100, 6, 9.0, 10.0),  -- IN_TRANSIT - 100 remaining
(10, 2009, 2, 300, 1, 13.0, NULL), -- PENDING - 300 remaining

-- Emergency orders
(10, 2010, 1, 75, 4, 11.0, NULL),  -- IN_PROGRESS - copper
(10, 2011, 2, 50, 4, 11.0, NULL);  -- IN_PROGRESS - silicone

-- ============================================================================
-- MATERIAL SUPPLIES - Current inventory
-- ============================================================================

-- Copper supplies (available for production)
INSERT INTO material_supplies (material_id, received_at, processed_at) VALUES
-- From order 2001 (500 units) - 200 used, 300 remaining
(1, 2.0, NULL), (1, 2.0, NULL), (1, 2.0, NULL), (1, 2.0, NULL), (1, 2.0, NULL),
(1, 2.0, NULL), (1, 2.0, NULL), (1, 2.0, NULL), (1, 2.0, NULL), (1, 2.0, NULL),
(1, 2.0, NULL), (1, 2.0, NULL), (1, 2.0, NULL), (1, 2.0, NULL), (1, 2.0, NULL),
(1, 2.0, NULL), (1, 2.0, NULL), (1, 2.0, NULL), (1, 2.0, NULL), (1, 2.0, NULL),
-- Additional copper from order 2002 (300 units) - 100 used, 200 remaining
(1, 4.0, NULL), (1, 4.0, NULL), (1, 4.0, NULL), (1, 4.0, NULL), (1, 4.0, NULL),
(1, 4.0, NULL), (1, 4.0, NULL), (1, 4.0, NULL), (1, 4.0, NULL), (1, 4.0, NULL),
(1, 4.0, NULL), (1, 4.0, NULL), (1, 4.0, NULL), (1, 4.0, NULL), (1, 4.0, NULL),
(1, 4.0, NULL), (1, 4.0, NULL), (1, 4.0, NULL), (1, 4.0, NULL), (1, 4.0, NULL);

-- Silicone supplies (available for production)
INSERT INTO material_supplies (material_id, received_at, processed_at) VALUES
-- From order 2006 (400 units) - 150 used, 250 remaining
(2, 2.0, NULL), (2, 2.0, NULL), (2, 2.0, NULL), (2, 2.0, NULL), (2, 2.0, NULL),
(2, 2.0, NULL), (2, 2.0, NULL), (2, 2.0, NULL), (2, 2.0, NULL), (2, 2.0, NULL),
(2, 2.0, NULL), (2, 2.0, NULL), (2, 2.0, NULL), (2, 2.0, NULL), (2, 2.0, NULL),
(2, 2.0, NULL), (2, 2.0, NULL), (2, 2.0, NULL), (2, 2.0, NULL), (2, 2.0, NULL),
(2, 2.0, NULL), (2, 2.0, NULL), (2, 2.0, NULL), (2, 2.0, NULL), (2, 2.0, NULL),
-- Additional silicone from order 2007 (200 units) - 50 used, 150 remaining
(2, 5.0, NULL), (2, 5.0, NULL), (2, 5.0, NULL), (2, 5.0, NULL), (2, 5.0, NULL),
(2, 5.0, NULL), (2, 5.0, NULL), (2, 5.0, NULL), (2, 5.0, NULL), (2, 5.0, NULL),
(2, 5.0, NULL), (2, 5.0, NULL), (2, 5.0, NULL), (2, 5.0, NULL), (2, 5.0, NULL);

-- Used materials (processed_at > 0)
INSERT INTO material_supplies (material_id, received_at, processed_at) VALUES
-- Used copper (300 units used in production)
(1, 2.0, 6.0), (1, 2.0, 6.0), (1, 2.0, 6.0), (1, 2.0, 6.0), (1, 2.0, 6.0),
(1, 2.0, 7.0), (1, 2.0, 7.0), (1, 2.0, 7.0), (1, 2.0, 7.0), (1, 2.0, 7.0),
(1, 4.0, 8.0), (1, 4.0, 8.0), (1, 4.0, 8.0), (1, 4.0, 8.0), (1, 4.0, 8.0),
-- Used silicone (200 units used in production)
(2, 2.0, 6.0), (2, 2.0, 6.0), (2, 2.0, 6.0), (2, 2.0, 6.0), (2, 2.0, 6.0),
(2, 2.0, 7.0), (2, 2.0, 7.0), (2, 2.0, 7.0), (2, 2.0, 7.0), (2, 2.0, 7.0),
(2, 5.0, 8.0), (2, 5.0, 8.0), (2, 5.0, 8.0), (2, 5.0, 8.0), (2, 5.0, 8.0);

-- ============================================================================
-- ELECTRONICS - Produced electronics inventory
-- ============================================================================

-- Available electronics (100 units produced from materials above)
INSERT INTO electronics (produced_at, electronics_status, sold_at) VALUES
-- Production batch 1 (day 6) - 50 units
(6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL),
(6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL),
(6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL),
(6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL),
(6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL),
(6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL),
(6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL),
(6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL),
(6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL),
(6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL), (6.0, 1, NULL),

-- Production batch 2 (day 7) - 50 units
(7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL),
(7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL),
(7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL),
(7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL),
(7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL),
(7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL),
(7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL),
(7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL),
(7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL),
(7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL), (7.0, 1, NULL);

-- Sold electronics (50 units sold to manufacturers)
INSERT INTO electronics (produced_at, electronics_status, sold_at) VALUES
-- Sold to Pear (25 units)
(3.0, 2, 9.0), (3.0, 2, 9.0), (3.0, 2, 9.0), (3.0, 2, 9.0), (3.0, 2, 9.0),
(3.0, 2, 9.0), (3.0, 2, 9.0), (3.0, 2, 9.0), (3.0, 2, 9.0), (3.0, 2, 9.0),
(3.0, 2, 9.0), (3.0, 2, 9.0), (3.0, 2, 9.0), (3.0, 2, 9.0), (3.0, 2, 9.0),
(3.0, 2, 9.0), (3.0, 2, 9.0), (3.0, 2, 9.0), (3.0, 2, 9.0), (3.0, 2, 9.0),
(3.0, 2, 9.0), (3.0, 2, 9.0), (3.0, 2, 9.0), (3.0, 2, 9.0), (3.0, 2, 9.0),
-- Sold to SumSang (25 units)
(4.0, 2, 10.0), (4.0, 2, 10.0), (4.0, 2, 10.0), (4.0, 2, 10.0), (4.0, 2, 10.0),
(4.0, 2, 10.0), (4.0, 2, 10.0), (4.0, 2, 10.0), (4.0, 2, 10.0), (4.0, 2, 10.0),
(4.0, 2, 10.0), (4.0, 2, 10.0), (4.0, 2, 10.0), (4.0, 2, 10.0), (4.0, 2, 10.0),
(4.0, 2, 10.0), (4.0, 2, 10.0), (4.0, 2, 10.0), (4.0, 2, 10.0), (4.0, 2, 10.0),
(4.0, 2, 10.0), (4.0, 2, 10.0), (4.0, 2, 10.0), (4.0, 2, 10.0), (4.0, 2, 10.0);

-- ============================================================================
-- ELECTRONICS ORDERS - Customer orders from phone manufacturers
-- ============================================================================

INSERT INTO electronics_orders (order_status, manufacturer_id, total_amount, remaining_amount, ordered_at, processed_at) VALUES
-- Completed orders
(5, 6, 25, 0, 8.0, 9.0),    -- Pear - 25 units (COMPLETED)
(5, 7, 25, 0, 9.0, 10.0),   -- SumSang - 25 units (COMPLETED)
(5, 6, 50, 0, 5.0, 6.0),    -- Pear - 50 units (COMPLETED)

-- Active orders
(1, 6, 75, 75, 13.0, NULL), -- Pear - 75 units (PENDING)
(1, 7, 100, 100, 14.0, NULL), -- SumSang - 100 units (PENDING)
(4, 6, 30, 30, 11.0, NULL), -- Pear - 30 units (IN_PROGRESS)

-- Future orders
(1, 7, 50, 50, 15.0, NULL), -- SumSang - 50 units (PENDING)
(1, 6, 40, 40, 15.0, NULL); -- Pear - 40 units (PENDING)
