# Document Management Architecture

## Overview
The Document Management system provides functionality for uploading, indexing, and searching documents. It relies on SQL Server's Full-Text Search capabilities to enable content-based retrieval (e.g., searching within the body of a PDF or Word document).

## Service-Oriented Document Handling Architecture
As part of the Metadata Standardization initiative, document metadata processing has been transitioned to a service-oriented architecture:

### 1. Metadata Service (`DocMetadata.MetadataService`)
A dedicated, centralized service (`DocMetadata` library) handles metadata extraction and standardization. 
- It ensures that file extensions are correctly formatted (always retaining the leading dot, e.g., `.pdf` instead of `pdf`).
- Centralizing this logic provides automated test coverage (`DocMetadata.Tests`), protecting the pipeline against future regressions where extensions might be stripped.
- Ensures SQL Server's Full-Text indexing `TYPE COLUMN` behaves correctly, as it natively requires the leading dot to identify the proper iFilter for extracting binary document contents.

### 2. Validation
Document binary signature validation is strictly tied to the corrected metadata format to ensure consistency and prevent upload of malformed files or unsupported document types.

## Database
- **Indexing:** The `Documents` table relies on the standard extension format (`.pdf`, `.docx`, etc.) for the Full-Text Index `TYPE COLUMN`.
- **Migration:** A `DataCorrection.sql` script is provided for standardizing legacy records. It includes a built-in migration report indicating how many records were updated.
