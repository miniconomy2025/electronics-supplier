-- Dummy data for materials (explicit IDs)
INSERT INTO materials (material_id, material_name) VALUES (1, 'copper');
INSERT INTO materials (material_id, material_name) VALUES (2, 'silicone');
INSERT INTO materials (material_id, material_name) VALUES (3, 'sand');

-- Dummy data for suppliers (explicit IDs)
INSERT INTO material_suppliers (supplier_id, supplier_name) VALUES (1, 'The Hand');
INSERT INTO material_suppliers (supplier_id, supplier_name) VALUES (2, 'Recycler');

-- Dummy data for manufacturers (explicit IDs)
INSERT INTO phone_manufacturers (manufacturer_id, manufacturer_name) VALUES (1, 'Pear');
INSERT INTO phone_manufacturers (manufacturer_id, manufacturer_name) VALUES (2, 'SumSang');

-- Dummy machines (using the add_machine procedure with 2 and 3 materials)
CALL add_machine('{"copper": 2, "silicone": 1}', 1000, NOW());
CALL add_machine('{"copper": 1, "silicone": 1, "sand": 2}', 1500, NOW());

-- Dummy supplies (unprocessed and processed)
INSERT INTO supplies (material_id, received_at, processed_at) VALUES (1, NOW() - INTERVAL '10 days', NULL);
INSERT INTO supplies (material_id, received_at, processed_at) VALUES (2, NOW() - INTERVAL '9 days', NOW() - INTERVAL '5 days');
INSERT INTO supplies (material_id, received_at, processed_at) VALUES (3, NOW() - INTERVAL '8 days', NULL);

-- Dummy material orders (explicit IDs, pending and completed)
INSERT INTO material_orders (order_id, supplier_id, ordered_at, received_at) VALUES (1, 1, NOW() - INTERVAL '7 days', NULL);
INSERT INTO material_orders (order_id, supplier_id, ordered_at, received_at) VALUES (2, 2, NOW() - INTERVAL '14 days', NOW() - INTERVAL '12 days');

-- Dummy material order items
INSERT INTO material_order_items (material_id, amount, order_id) VALUES (1, 5, 1);
INSERT INTO material_order_items (material_id, amount, order_id) VALUES (2, 3, 1);
INSERT INTO material_order_items (material_id, amount, order_id) VALUES (3, 2, 2);

-- Complete a material order (should add supplies)
CALL complete_material_order(1);

-- Dummy machine orders (explicit IDs, pending and completed)
INSERT INTO machine_orders (order_id, supplier_id, ordered_at, received_at) VALUES (1, 1, NOW() - INTERVAL '20 days', NULL);
INSERT INTO machine_orders (order_id, supplier_id, ordered_at, received_at) VALUES (2, 2, NOW() - INTERVAL '30 days', NOW() - INTERVAL '25 days');

-- Complete a machine order (should add a machine)
CALL complete_machine_order(1, 2000);

-- Dummy electronics (in stock and sold)
INSERT INTO electronics (produced_at, sold_at) VALUES (NOW() - INTERVAL '15 days', NULL);
INSERT INTO electronics (produced_at, sold_at) VALUES (NOW() - INTERVAL '14 days', NOW() - INTERVAL '10 days');
INSERT INTO electronics (produced_at, sold_at) VALUES (NOW() - INTERVAL '13 days', NULL);

-- Dummy electronics orders (explicit IDs, pending and processed)
INSERT INTO electronics_orders (order_id, manufacturer_id, amount, ordered_at, processed_at) VALUES (1, 1, 1, NOW() - INTERVAL '5 days', NULL);
INSERT INTO electronics_orders (order_id, manufacturer_id, amount, ordered_at, processed_at) VALUES (2, 2, 2, NOW() - INTERVAL '20 days', NOW() - INTERVAL '18 days');

-- Process an electronics order (should mark electronics as sold)
CALL process_electronics_order(1);

-- Dummy lookup values
INSERT INTO lookup_values (electronics_price) VALUES (499.99);
INSERT INTO lookup_values (electronics_price) VALUES (599.99);

CALL update_material(1, 'copperX');
CALL update_machine(1, 2, NULL, NULL); -- 2 = BROKEN
CALL update_material_order(1, NULL, NOW(), NULL);
CALL update_electronic(1, NULL, NOW());
