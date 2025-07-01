-- Dummy data for materials
INSERT INTO materials (material_name) VALUES ('copper');
INSERT INTO materials (material_name) VALUES ('silicone');
INSERT INTO materials (material_name) VALUES ('sand');

-- Dummy data for suppliers
INSERT INTO material_suppliers (supplier_name) VALUES ('The Hand');
INSERT INTO material_suppliers (supplier_name) VALUES ('Recycler');

-- Dummy data for manufacturers
INSERT INTO phone_manufacturers (manufacturer_name) VALUES ('Pear');
INSERT INTO phone_manufacturers (manufacturer_name) VALUES ('SumSang');

-- Dummy machines (using the add_machine procedure with 2 and 3 materials)
CALL add_machine('{"copper": 2, "silicone": 1}', 1000, NOW());
CALL add_machine('{"copper": 1, "silicone": 1, "sand": 2}', 1500, NOW());

-- Dummy supplies (unprocessed and processed)
INSERT INTO supplies (material_id, received_at, processed_at) VALUES (1, NOW() - INTERVAL '10 days', NULL);
INSERT INTO supplies (material_id, received_at, processed_at) VALUES (2, NOW() - INTERVAL '9 days', NOW() - INTERVAL '5 days');
INSERT INTO supplies (material_id, received_at, processed_at) VALUES (3, NOW() - INTERVAL '8 days', NULL);

-- Dummy material orders (pending and completed)
INSERT INTO material_orders (supplier_id, ordered_at, received_at) VALUES (1, NOW() - INTERVAL '7 days', NULL);
INSERT INTO material_orders (supplier_id, ordered_at, received_at) VALUES (2, NOW() - INTERVAL '14 days', NOW() - INTERVAL '12 days');

-- Dummy material order items
INSERT INTO material_order_items (material_id, amount, order_id) VALUES (1, 5, 1);
INSERT INTO material_order_items (material_id, amount, order_id) VALUES (2, 3, 1);
INSERT INTO material_order_items (material_id, amount, order_id) VALUES (3, 2, 2);

-- Complete a material order (should add supplies)
CALL complete_material_order(1);

-- Dummy machine orders (pending and completed)
INSERT INTO machine_orders (supplier_id, ordered_at, received_at) VALUES (1, NOW() - INTERVAL '20 days', NULL);
INSERT INTO machine_orders (supplier_id, ordered_at, received_at) VALUES (2, NOW() - INTERVAL '30 days', NOW() - INTERVAL '25 days');

-- Complete a machine order (should add a machine)
CALL complete_machine_order(1, 2000);

-- Dummy electronics (in stock and sold)
INSERT INTO electronics (produced_at, sold_at) VALUES (NOW() - INTERVAL '15 days', NULL);
INSERT INTO electronics (produced_at, sold_at) VALUES (NOW() - INTERVAL '14 days', NOW() - INTERVAL '10 days');
INSERT INTO electronics (produced_at, sold_at) VALUES (NOW() - INTERVAL '13 days', NULL);

-- Dummy electronics orders (pending and processed)
INSERT INTO electronics_orders (manufacturer_id, amount, ordered_at, processed_at) VALUES (1, 1, NOW() - INTERVAL '5 days', NULL);
INSERT INTO electronics_orders (manufacturer_id, amount, ordered_at, processed_at) VALUES (2, 2, NOW() - INTERVAL '20 days', NOW() - INTERVAL '18 days');

-- Process an electronics order (should mark electronics as sold)
CALL process_electronics_order(1);

-- Dummy lookup values
INSERT INTO lookup_values (electronics_price) VALUES (499.99);
INSERT INTO lookup_values (electronics_price) VALUES (599.99);

CALL update_material(1, 'copperX');
CALL update_machine(1, 'BROKEN', NULL, NULL);
CALL update_material_order(1, NULL, NOW(), NULL);
CALL update_electronic(1, NULL, NOW()); 