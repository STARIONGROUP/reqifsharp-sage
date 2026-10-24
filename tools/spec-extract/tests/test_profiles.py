import pytest

from spec_extract.profiles import GENERIC_PROFILE, profile_for


@pytest.mark.parametrize("document", ["ReqIF", "reqif", " REQIF "])
def test_profile_for_finds_the_reqif_profile_case_insensitively(document: str) -> None:
    profile = profile_for(document)

    assert profile.name == "ReqIF"
    assert profile.license == "OMG-specification"
    assert profile.redistributable is False
    assert profile.normative_pattern is not None


def test_profile_for_the_implementation_guide_is_informative_and_restricted() -> None:
    profile = profile_for("PSI-Guide")

    assert profile.license == "prostep-ivip-restricted"
    assert profile.redistributable is False
    assert profile.normative_pattern is None
    assert profile.recommendation_pattern is not None
    assert profile.recommendation_pattern.search("Tools SHOULD do this")


def test_profile_for_an_unknown_document_is_the_generic_non_redistributable_profile() -> None:
    profile = profile_for("SomethingElse")

    assert profile is GENERIC_PROFILE
    assert profile.license == "unknown"
    assert profile.redistributable is False
    assert profile.toc_pages is None


def test_omg_normative_keywords_include_upper_case_rfc_2119_words_but_not_lower_case_should() -> None:
    pattern = profile_for("ReqIF").normative_pattern

    assert pattern.search("An element MAY carry a default value.")
    assert pattern.search("Exporters shall write UTF-8.")
    assert not pattern.search("Exporters should write UTF-8.")
    assert not pattern.search("The mustard example.")
