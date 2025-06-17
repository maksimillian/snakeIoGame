CREATE DATABASE chat;

INSERT INTO public."Profile"
(id, "lastSeenAt")
VALUES('e88c09cc-0559-4309-a6ed-5b5e0496d218', NULL);
INSERT INTO public."Profile"
(id, "lastSeenAt")
VALUES('32b8da77-a3f9-4671-ab2b-3e7cca8569c7', NULL);

INSERT INTO public."ChatSettings"
("notify", "profileId")
VALUES(NULL, 'e88c09cc-0559-4309-a6ed-5b5e0496d218');
INSERT INTO public."ChatSettings"
("notify", "profileId")
VALUES(NULL, '32b8da77-a3f9-4671-ab2b-3e7cca8569c7');