﻿ALTER TABLE [dbo].[carrot_ContentComment]
    ADD CONSTRAINT [PK_carrot_ContentComment] PRIMARY KEY NONCLUSTERED ([ContentCommentID] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF) ON [PRIMARY];

